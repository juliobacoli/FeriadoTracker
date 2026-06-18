# Plano: Confirmação de Entrega (ACK) e Reenvio de Notificações Push

> **Status:** 🔴 CANCELADO / PIVOTADO
> **Data da Revisão:** 18 de Junho de 2026
> 
> **Decisão Arquitetural (ADR):** 
> Após revisão técnica do plano original, a equipe concluiu que a construção de um sistema customizado de ACKs e retentativas na camada de aplicação configuraria *overengineering* (engenharia excessiva). 
> O problema de usuários offline em finais de semana pode ser resolvido de forma nativa e sem custo computacional delegando o trabalho de fila (*store-and-forward*) para o próprio Push Service (Google FCM/Apple APNs). 
> 
> **Ação Adotada:** Em vez de implementar a arquitetura abaixo, o sistema utilizará um **TTL Dinâmico** no Web Push. O `TTL` da mensagem será calculado para expirar apenas no exato momento do final do dia do feriado. Dessa forma, o próprio intermediário garante a entrega caso o usuário ligue o PC dias após o envio inicial, com zero necessidade de alterações no banco de dados ou Service Worker.
> 
> *O texto original do plano abaixo foi mantido apenas para registro histórico das alternativas consideradas.*

> **Pré-requisito (Histórico):** PR #7 (TTL 24h + data absoluta) — já em produção.
> **Objetivo deste documento (Histórico):** servir de guia completo para implementar, testar
> e lançar a melhoria com segurança. Escrito para ser entendido sem conhecimento
> prévio dos termos técnicos — cada conceito é explicado na primeira vez que aparece.

---

## 1. Contexto — por que isso existe

### O que já foi resolvido (PR #7)

- Notificação com texto "Faltam 5 dias" chegava atrasada e mentia. **Resolvido:**
  texto agora usa data absoluta ("Corpus Christi em 04 de junho.").
- Mensagem ficava guardada no intermediário por até 4 semanas. **Resolvido:**
  TTL de 24h — não entregou no dia, é descartada.

### O buraco que sobrou

O sistema envia **uma única vez** por feriado por usuário. Se essa única
tentativa for descartada (usuário offline nas 24h seguintes ao envio),
**o usuário nunca recebe a notificação daquele feriado**. Não há segunda chance.

### Quando isso acontece na prática

O envio ocorre quando o feriado entra na janela de 5 dias (`WebPush:DaysAhead=5`),
às 08h de Brasília. Logo:

| Feriado cai em | Envio acontece em | Risco para usuário só-desktop |
|---|---|---|
| Segunda | Quarta anterior | Baixo (dia útil, browser aberto) |
| Quinta | **Sábado anterior** | **Alto** (browser fechado no fim de semana) |
| Sexta | **Domingo anterior** | **Alto** (idem) |

No celular o risco é quase zero: Android e iPhone recebem push no nível do
sistema operacional, mesmo com o navegador fechado. No computador, Chrome,
Edge e Firefox precisam estar abertos (ou em segundo plano) para receber.

---

## 2. Conceitos — o vocabulário deste plano

| Termo | O que é, em uma frase |
|---|---|
| **Service worker** (`wwwroot/sw.js`) | Script que o site instala no navegador do usuário e que continua rodando mesmo com a aba fechada; é ele que recebe o push e desenha a notificação na tela. |
| **ACK** (acknowledgement) | "Aviso de recebimento". O navegador devolve ao servidor a mensagem "recebi, pode parar de mandar". Equivalente ao ✓✓ do WhatsApp. |
| **Push service** | Intermediário (carteiro) entre o servidor e o navegador: Google FCM (Chrome/Android), Apple APNs (Safari/iOS), Mozilla autopush (Firefox). O servidor entrega a carta ao carteiro; não fala direto com o navegador. |
| **At-most-once → at-least-once** | Hoje o sistema entrega "no máximo uma vez" (pode ser zero). Com este plano passa a entregar "pelo menos uma vez" (pode, raramente, ser duas). |
| **Token de capacidade** | Código aleatório impossível de adivinhar (um GUID) que funciona como senha de uso único: quem o apresenta prova que recebeu a notificação à qual ele pertence. Dispensa login ou outro tipo de autenticação no endpoint de ACK. |
| **Idempotência** | Propriedade de uma operação que pode ser repetida sem efeito adicional: confirmar o mesmo ACK duas vezes não pode causar erro nem efeito colateral. |
| **Migration** | Script gerado pelo Entity Framework que altera o schema do banco (criar coluna, derrubar índice) de forma versionada. |

### A analogia que resume tudo

Hoje: carta comum. O correio diz "peguei sua carta" e o sistema anota
"entregue". Se a carta se perder, ninguém fica sabendo.

Com este plano: **carta registrada**. Dentro da carta vai um código. Quando o
navegador recebe, devolve o código ao servidor ("chegou!"). Sem código de volta
em 24h, o servidor manda outra carta. Repete até confirmar ou o feriado passar.

---

## 3. A solução em uma figura

```
        (job diário, 08h BRT)
┌──────────┐  1. envia push        ┌──────────────┐  2. entrega   ┌───────────┐
│ Servidor │ ────────────────────► │ Push service │ ────────────► │ Navegador │
│ (Railway)│    payload contém     │  (FCM/APNs/  │               │ (sw.js)   │
│          │    ackToken: "abc123" │   Mozilla)   │               │           │
└──────────┘                       └──────────────┘               └─────┬─────┘
      ▲                                                                 │
      │  3. POST /api/push/ack { token: "abc123" }                      │
      └─────────────────────────────────────────────────────────────────┘
      4. Servidor grava DeliveredAtUtc no NotificationLog
      5. Job do dia seguinte: tem DeliveredAtUtc? → não reenvia.
         Não tem e o envio foi há mais de 24h? → reenvia (novo token).
         Feriado já passou? → desiste.
```

---

## 4. Mudanças, arquivo por arquivo

### 4.1 `FeriadoTracker.Web/Models/NotificationLog.cs`

**O quê:** duas propriedades novas.

```csharp
public string AckToken { get; set; } = string.Empty;  // GUID gerado no envio
public DateTime? DeliveredAtUtc { get; set; }          // null = nunca confirmado
```

**Por quê:** `AckToken` identifica qual envio está sendo confirmado;
`DeliveredAtUtc` é a prova de entrega. Nullable porque envio sem confirmação
é o estado inicial de todo log.

**Cascade já existente (não mexer, mas saber):** `NotificationLog` tem FK para
`PushSubscription` com `OnDelete(DeleteBehavior.Cascade)`
(`AppDbContext.cs`, linhas 23-27). Quando uma subscription é removida (outcome
`Gone`), seus logs — e os `AckToken` deles — somem junto. Logo não sobram
tokens órfãos no banco apontando para subscriptions inexistentes.

### 4.2 Schema: editar `AppDbContext.cs` e gerar a migration

> **Importante:** os índices do `NotificationLog` são definidos por Fluent API
> em `AppDbContext.OnModelCreating`, **não** em uma migration escrita à mão.
> O fluxo correto é: (1) editar `AppDbContext.cs`, (2) rodar
> `dotnet ef migrations add AckEReenvio` — o EF gera a migration a partir do
> diff do modelo. Nunca o contrário.

**Passo a — editar `FeriadoTracker.Web/Data/AppDbContext.cs`:**

Hoje (linhas 19-21):

```csharp
modelBuilder.Entity<NotificationLog>()
    .HasIndex(n => new { n.SubscriptionId, n.FeriadoId })
    .IsUnique();
```

Passa a:

```csharp
// Não-único: agora há uma linha por tentativa de envio. A consulta de status
// (GetSendStatusAsync) continua indexada e rápida.
modelBuilder.Entity<NotificationLog>()
    .HasIndex(n => new { n.SubscriptionId, n.FeriadoId });

// Único, mas FILTRADO: ver "Cuidado com dados existentes" abaixo.
modelBuilder.Entity<NotificationLog>()
    .HasIndex(n => n.AckToken)
    .IsUnique()
    .HasFilter("\"AckToken\" <> ''");
```

**Passo b — gerar a migration:** `dotnet ef migrations add AckEReenvio`. Resultado
esperado: duas colunas novas (`AckToken` TEXT not null default `''`,
`DeliveredAtUtc` TEXT null), índice `(SubscriptionId, FeriadoId)` deixa de ser
único, índice único filtrado em `AckToken`.

**⚠ Cuidado com dados existentes (senão a migration QUEBRA):**
já há `NotificationLog`s em produção (ex.: os 9 do Corpus Christi). Todos
recebem `AckToken = ''` no default. Um índice único **simples** em `AckToken`
rejeitaria esses múltiplos `''` duplicados → criação do índice falha no deploy.
Por isso o índice é **único filtrado** (`HasFilter("\"AckToken\" <> ''")`):
ele ignora as linhas com token vazio (todas históricas) e só garante unicidade
para tokens reais gerados daqui pra frente. SQLite suporta índice parcial.
Alternativa equivalente: backfill das linhas antigas com GUIDs na migration —
mais trabalho, mesmo efeito; o filtro é mais simples.

**Esclarecimento — o que realmente proíbe reenvio hoje:** não é o índice único.
É a verificação **em código** dentro de `SendDailyAsync`: `GetAlreadySentAsync`
consulta os logs e `alreadySent.Contains((sub.Id, feriado.Id))` faz o `continue`
antes de qualquer insert. O índice único é só uma trava de integridade no banco
— precisa cair porque, mantido, ele estouraria no insert da 2ª tentativa; mas a
**lógica** de pular/reenviar vive no código (seção 4.3-b), não no índice.

**Logs antigos:** ficam com `AckToken = ''` e `DeliveredAtUtc = null`. A regra
de reenvio ignora feriados passados, então linhas históricas não disparam nada.

### 4.3 `FeriadoTracker.Web/Services/HolidayPushSender.cs`

Hoje o método `SendDailyAsync` pergunta: *"já existe log para
(subscription, feriado)?"* — se sim, pula. As mudanças:

**a) Payload deixa de ser por feriado e passa a ser por subscription.**
Hoje `BuildPayload(feriado, today)` é chamado uma vez por feriado (linha ~42) e
reaproveitado para todas as subscriptions. Como o `ackToken` é único por
tentativa, o payload passa a ser montado **dentro** do loop de subscriptions,
recebendo o token recém-gerado:

```csharp
var ackToken = Guid.NewGuid().ToString("N");
var payload = BuildPayload(feriado, today, ackToken);
```

E `BuildPayload` inclui o campo no JSON: `ackToken = ackToken`.

**b) A regra de pular muda — atrás da feature flag** (seção 4.7).
Com `WebPush:AckResendEnabled = false` (default), a regra atual permanece:
existe log → pula. Com a flag ligada, a decisão passa a ser:

- existe log com `DeliveredAtUtc != null`? → **confirmado, pula** (conta como `skipped`);
- senão, existe log com `SentAtUtc` há **menos de 24h**? → **aguardando, pula**
  (o TTL ainda pode entregar a tentativa anterior);
- senão → **envia de novo** (nova linha no log, novo token).

A condição "feriado já passou" não precisa de código novo: a janela de busca de
feriados (`Data >= today`) já exclui feriados passados naturalmente.

**Estrutura pronta para implementação:**

`SendSettings` é um DTO público em `FeriadoTracker.Web/Dtos/SendSettings.cs`
(hoje `public sealed record SendSettings(int DaysAhead);`). Editar esse arquivo
para acrescentar o campo da flag:

```csharp
public sealed record SendSettings(int DaysAhead, bool AckResendEnabled);
```

`LoadSettings()` ganha uma leitura (flag ausente → `false` → comportamento atual):

```csharp
var ackResendEnabled = config.GetValue<bool>("WebPush:AckResendEnabled");
// ...
return new SendSettings(daysAhead, ackResendEnabled);
```

`GetAlreadySentAsync` muda o retorno de `HashSet<(int, int)>` para um
dicionário com o status do **log mais recente** de cada par:

```csharp
private sealed record SendStatus(bool Delivered, DateTime LastSentAtUtc);

private async Task<Dictionary<(int SubscriptionId, int FeriadoId), SendStatus>> GetSendStatusAsync(
    List<Feriado> feriados, List<PushSubscription> subscriptions, CancellationToken ct)
{
    // mesma consulta de hoje, acrescentando DeliveredAtUtc e SentAtUtc no Select;
    // agrupa por (SubscriptionId, FeriadoId) e materializa:
    //   Delivered     = qualquer linha do grupo com DeliveredAtUtc != null
    //   LastSentAtUtc = maior SentAtUtc do grupo
}
```

E o ponto de decisão dentro do loop de subscriptions:

```csharp
var status = sendStatus.GetValueOrDefault((sub.Id, feriado.Id)); // null = nunca enviou

var skip = settings.AckResendEnabled
    ? status is not null
        && (status.Delivered
            || time.GetUtcNow().UtcDateTime - status.LastSentAtUtc < TimeSpan.FromHours(24))
    : status is not null; // regra atual: qualquer log → pula

if (skip) { skipped++; continue; }
```

A comparação de 24h usa o `TimeProvider` já injetado — nos testes, o
`FakeTimeProvider` controla o relógio sem mock adicional.

**c) Ao gravar o log** (`case PushSendOutcome.Success`), incluir o `AckToken`
usado no payload daquela tentativa.

### 4.4 `FeriadoTracker.Web/Controllers/PushController.cs`

**O quê:** uma action nova.

```csharp
[HttpPost("ack")]
public async Task<IActionResult> Ack([FromBody] AckDto dto, CancellationToken ct)
```

Comportamento:

- `dto.Token` vazio ou em branco → `400 BadRequest`.
- Busca `NotificationLog` com `AckToken == dto.Token`. Não achou → `404 NotFound`.
- Achou com `DeliveredAtUtc` já preenchido → `204 NoContent` **sem alterar nada**
  (idempotência: o service worker pode reenviar o ACK em retry).
- Achou sem confirmação → grava `DeliveredAtUtc = agora (UTC)` → `204 NoContent`.

**Atenção — sem `[ValidateAntiForgeryToken]`, e isso é deliberado:**
`Subscribe`/`Unsubscribe` usam antiforgery token porque são chamados por
JavaScript de página, que tem acesso ao token embutido no HTML. O service worker
roda **fora de qualquer página** — não tem HTML, não tem cookie de sessão
garantido, não tem como obter o antiforgery token. A segurança do endpoint vem
do próprio `AckToken`: um GUID de 32 caracteres hexadecimais que só existe em
dois lugares (o banco e o payload criptografado entregue àquele navegador).
Quem o apresenta, comprovadamente recebeu o push. Forjar exigiria adivinhar
2^122 combinações. O rate limiting do controller (`[EnableRateLimiting("push")]`)
já se aplica à action nova e bloqueia tentativas de força bruta.

### 4.5 `FeriadoTracker.Web/Dtos/` — arquivo novo `AckDto.cs`

```csharp
public record AckDto(string Token);
```

(Seguir o padrão dos DTOs existentes na pasta — conferir se são `record` ou
classe com propriedades — e replicar.)

### 4.6 `FeriadoTracker.Web/wwwroot/sw.js`

Duas mudanças no listener de `push` (linhas 9–26):

**a) Enviar o ACK.** Dentro do `event.waitUntil`, após exibir a notificação:

```javascript
const ackPromise = data.ackToken
    ? fetch('/api/push/ack', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ token: data.ackToken })
      }).catch(() => { /* falha silenciosa: o reenvio de amanhã cobre */ })
    : Promise.resolve();

event.waitUntil(Promise.all([
    self.registration.showNotification(title, options),
    ackPromise
]));
```

Raciocínio do `catch` vazio: se o ACK falhar, o pior caso é o usuário receber a
notificação duas vezes — preferível a quebrar a exibição.

**b) Colapsar reenvios com `tag`.** Em `options`:

```javascript
tag: data.tag || undefined,
```

E o servidor inclui no payload `tag = $"feriado-{feriado.Id}"`. Efeito: se duas
tentativas chegarem (caso raro de ACK perdido), a segunda **substitui** a
primeira na tela em vez de empilhar. Custo: uma linha de cada lado.

### 4.7 Feature flag — `WebPush:AckResendEnabled`

**O quê:** variável de configuração booleana (env var na Railway), default `false`.
Lida em `HolidayPushSender.LoadSettings()` junto com as demais configs de WebPush
(estrutura de código na seção 4.3-b).

**Como ligar/desligar na Railway:** painel → service → Variables →
`WebPush__AckResendEnabled=true` (underscore duplo equivale a `:` no .NET).
Atenção: alterar env var na Railway **reinicia o container** (~30s). Não é
deploy de código, e para um job que roda 1x/dia o restart é irrelevante.

**Localmente:** `WebPush__AckResendEnabled=true` no `FeriadoTracker.Web/.env`
(necessário para o cenário B do teste funcional).

**O que ela controla — e o que ela NÃO controla:**

| Comportamento | Controlado pela flag? | Por quê |
|---|---|---|
| Regra de **reenvio** (tabela da seção 5) | **Sim** — flag off restaura a regra antiga: "existe qualquer log → pula" | É o único comportamento com risco real (duplicatas) |
| Token no payload | Não — sempre ativo | Inerte: campo extra no JSON não muda nada para quem não o lê |
| Endpoint de ACK + gravação de `DeliveredAtUtc` | Não — sempre ativo | Inerte e útil: coleta métrica de entrega **antes** de ligar o reenvio |
| `tag` no service worker | Não — sempre ativo | Inerte: só tem efeito se houver duas notificações, o que exige reenvio ligado |
| Migration (colunas + índices) | Não — schema é permanente | Aditiva, não quebra código antigo nem novo |

**Estratégia de lançamento que isso habilita:**

1. Deploy com flag **off** → sistema se comporta exatamente como hoje, mas passa
   a registrar `DeliveredAtUtc` de quem recebe.
2. Observar 1–2 ciclos reais de feriado: que fração dos envios é confirmada?
   (Consulta: `SELECT COUNT(*), COUNT(DeliveredAtUtc) FROM NotificationLogs WHERE ...`)
3. Ligar a flag (trocar env var na Railway — sem deploy) → reenvio ativo.
4. Qualquer problema → desligar a flag. Sem deploy, sem migration reversa, sem rollback de código.

### 4.8 O que **não** muda

- `VapidWebPushClient.cs` — TTL e envio ficam como estão.
- `NotificationTemplates.cs` — textos ficam como estão.
- Agendamento do job — mesmo horário, mesma frequência.
- `Program.cs` — nenhum serviço novo para registrar (a flag entra via
  `IConfiguration`, que já é injetado no `HolidayPushSender`).

---

## 5. Tabela de decisão do reenvio

**Pré-condição:** `WebPush:AckResendEnabled = true`. Com a flag desligada, a
regra é a atual — "existe qualquer log para (subscription, feriado) → pula" —
e a tabela abaixo não se aplica.

Com a flag ligada, para cada par (subscription, feriado na janela), o job decide:

| Situação no banco | Decisão | Por quê |
|---|---|---|
| Nenhum log | **Envia** (1ª tentativa) | Comportamento atual preservado |
| Log com `DeliveredAtUtc` preenchido | **Pula** | Usuário confirmou recebimento |
| Log sem ACK, enviado há **menos de 24h** | **Pula** | TTL ainda pode entregar; reenviar agora poderia duplicar |
| Log sem ACK, enviado há **24h ou mais** | **Reenvia** (nova linha, novo token) | Tentativa anterior foi descartada pelo TTL |
| Feriado já passou | Nem entra na consulta | Janela `Data >= today` exclui naturalmente |

**Limite natural de tentativas:** a janela é de 5 dias e o reenvio respeita 24h
entre tentativas → no máximo ~5 tentativas por feriado, depois o feriado passa
e o ciclo morre sozinho. Não precisa de contador de tentativas.

---

## 6. Casos de borda

| Caso | O que acontece | Aceitável? |
|---|---|---|
| Push entregue, mas ACK falhou (rede caiu no exato instante) | Job reenvia no dia seguinte; usuário vê a notificação de novo — com `tag`, a nova substitui a antiga se ainda estiver na bandeja | Sim — raro e inofensivo |
| Usuário recebeu e fechou a notificação; reenvio chega no dia seguinte | Notificação "repetida" na tela | Sim — só ocorre se o ACK falhou; ver acima |
| Dois feriados na janela, iPhone offline | APNs guarda só 1 notificação por app: a segunda sobrescreve a primeira | Limitação da Apple, fora do nosso controle; o reenvio mitiga (cada dia reenvia o que falta) |
| ACK chega duas vezes (retry do browser) | Endpoint é idempotente: segunda chamada devolve 204 sem alterar nada | Sim |
| Atacante chuta tokens no endpoint | GUID de 122 bits + rate limiting → inviável | Sim |
| Logs antigos (pré-migration) sem token | `AckToken = ''`, feriados já passados → nunca entram na regra de reenvio | Sim |

---

## 7. Ferramentas — o que precisa ser instalado

**Nada novo no projeto.** Confirmado item a item:

| Necessidade | Como é atendida | Pacote novo? |
|---|---|---|
| Coluna nova + índice | EF Core migrations (já no projeto) | Não |
| Geração de token | `Guid.NewGuid()` (biblioteca padrão .NET) | Não |
| Endpoint de ACK | ASP.NET Core controller (já existe `PushController`) | Não |
| Fetch no service worker | JavaScript puro, API nativa do browser | Não |
| Testes de unidade/integração | xUnit + EF InMemory + FakeTimeProvider (já no `.csproj` de testes) | Não |
| Teste funcional local | `DevPushController` (já existe, só compila em DEBUG) + DevTools do Chrome | Não |
| Aplicar migration em produção | Já acontece no deploy (conferir `Program.cs` — `Migrate()` no startup) | Não |

Única ferramenta externa usada (e já usada antes): `dotnet ef` CLI para gerar a
migration. Se não estiver instalada: `dotnet tool install --global dotnet-ef`.

---

## 8. Plano de testes

### 8.1 Testes de unidade (xUnit, sem banco, sem HTTP)

**Pré-condição de testabilidade:** `BuildPayload` é hoje `private static` em
`HolidayPushSender`. Para testá-lo diretamente como unidade, expor como
`internal static` e adicionar no `.csproj` da Web:
`<InternalsVisibleTo Include="FeriadoTracker.Web.Tests" />` (ou
`[assembly: InternalsVisibleTo("FeriadoTracker.Web.Tests")]`). Sem isso, os dois
testes abaixo só são viáveis via integração (capturando o payload pelo
`FakePushClient`, como o teste atual já faz) — caminho aceitável e mais barato.

| Teste | O que garante |
|---|---|
| Payload contém `ackToken` e `tag` | Campos novos chegam ao JSON |
| Tokens de duas tentativas são diferentes | Cada reenvio é rastreável individualmente |

*(A regra de decisão do reenvio vive dentro do `HolidayPushSender`, que depende
de banco — ela é coberta nos testes de integração abaixo. Se durante a
implementação a regra for extraída para método estático puro, ganha testes de
unidade diretos — decisão a tomar na hora, não é pré-requisito.)*

### 8.2 Testes de integração (xUnit + EF InMemory + fakes — padrão já usado em `HolidayPushSenderTests`)

**Infraestrutura de teste:** o helper `BuildConfig` existente em
`HolidayPushSenderTests` ganha o parâmetro `bool ackResendEnabled = false`
(mesmo padrão do `withVapid: false` já usado), adicionando
`["WebPush:AckResendEnabled"] = ackResendEnabled.ToString()` ao dicionário.
Nenhum mock novo: relógio via `FakeTimeProvider`, banco via EF InMemory,
push via `FakePushClient` — tudo já existe.

**Em `HolidayPushSenderTests.cs` (casos novos):**

| Teste | Cenário montado | Resultado esperado |
|---|---|---|
| `NaoReenviaComFlagDesligada` | Log de ontem sem ACK, `AckResendEnabled=false` | 0 envios — comportamento atual preservado |
| `ReenviaQuandoSemAckApos24h` | Flag ligada; log de ontem, `DeliveredAtUtc = null`, relógio fake avança 24h | 1 envio novo, 2ª linha no log, token diferente |
| `NaoReenviaQuandoAckRegistrado` | Log de ontem com `DeliveredAtUtc` preenchido | 0 envios, contado em `skipped` |
| `NaoReenviaAntesDe24h` | Log de hoje 08h, relógio fake às 20h do mesmo dia | 0 envios |
| `NaoReenviaFeriadoPassado` | Log sem ACK, feriado foi ontem | 0 envios (feriado fora da janela) |
| `PayloadDeCadaSubscriptionTemTokenProprio` | 2 subscriptions, 1 feriado | 2 payloads com tokens distintos |

**Em `PushControllerTests.cs` (casos novos):**

| Teste | Cenário | Resultado esperado |
|---|---|---|
| `Ack_MarcaEntregaComTokenValido` | Log existente, token correto | 204; `DeliveredAtUtc` preenchido com o relógio fake |
| `Ack_Retorna404ComTokenDesconhecido` | Token que não existe no banco | 404; nada alterado |
| `Ack_Retorna400SemToken` | Body com token vazio | 400 |
| `Ack_EhIdempotente` | Mesmo token confirmado duas vezes | 2ª chamada: 204; `DeliveredAtUtc` **não** muda (mantém o primeiro valor) |

### 8.3 Teste funcional — roteiro manual completo

> Objetivo: você executar de ponta a ponta, ver com os próprios olhos e ter
> segurança para lançar. Tempo estimado: 30–40 min.

**Preparação (uma vez):**

1. Rodar local em DEBUG: `dotnet run --project FeriadoTracker.Web`
2. Abrir `https://localhost:<porta>` no Chrome
3. Ativar notificações no site (botão da UI) — confere no DevTools →
   `Application` → `Service Workers` que o `sw.js` está ativo
4. Garantir um feriado dentro da janela de 5 dias no banco local
   (`FeriadoTracker.Web/Data/feriados.db`). Se não houver, inserir um de teste:
   ```sql
   INSERT INTO Feriados (Nome, Data, Tipo) VALUES ('Teste ACK', '<hoje+3 dias>', 'Nacional');
   ```
5. **Configurar VAPID real no `.env` local.** Sem `WebPush:VapidPublicKey`,
   `WebPush:VapidPrivateKey` e `WebPush:Subject` preenchidos, `LoadSettings()`
   retorna `null` e **nada é enviado** — o teste não sai do lugar. Gerar um par
   de chaves de teste (ex.: `VapidHelper.GenerateVapidKeys()`) e colocar no
   `FeriadoTracker.Web/.env`. Essas chaves não precisam ser as de produção.

**Cenário A — caminho feliz (envio → recebimento → ACK):**

1. Disparar o job manualmente: `POST https://localhost:<porta>/api/dev/push/trigger`
   (via Postman/curl; endpoint só existe em DEBUG)
2. ✅ Notificação aparece na tela com texto de data absoluta
3. ✅ Aba `Network` do DevTools (contexto do service worker): request
   `POST /api/push/ack` com status 204
4. ✅ No banco: `SELECT AckToken, DeliveredAtUtc FROM NotificationLogs;` —
   `DeliveredAtUtc` preenchido
5. Disparar o trigger de novo → ✅ resposta mostra `skipped` ≥ 1 e **nenhuma**
   notificação nova aparece

**Cenário B — reenvio após falha de entrega** (exige `WebPush__AckResendEnabled=true` no `.env`):

1. Limpar logs: `DELETE FROM NotificationLogs;`
2. DevTools → `Application` → `Service Workers` → marcar **Offline** (o checkbox
   do contexto do SW; impede o **browser** de receber o push)
3. Disparar o trigger → o servidor envia normalmente ao push service real (o
   servidor não está offline, só o browser), mas o browser não recebe → nenhuma
   notificação aparece e nenhum ACK é enviado
4. ✅ No banco: linha nova com `DeliveredAtUtc = null`
5. Simular passagem de 24h: `UPDATE NotificationLogs SET SentAtUtc = datetime(SentAtUtc, '-25 hours'), SentDate = date(SentDate, '-1 day');`
6. Voltar `Network` para **Online**
7. Disparar o trigger → ✅ notificação chega agora; ✅ banco tem **2ª linha** de
   log, e ela ganha `DeliveredAtUtc` via ACK
8. Disparar de novo → ✅ `skipped`, sem nova notificação (ciclo fechou)

**Cenário C — idempotência do ACK (proteção contra duplicata):**

1. Pegar um `AckToken` do banco
2. `POST /api/push/ack` com esse token, duas vezes seguidas (curl/Postman)
3. ✅ Ambas retornam 204; `DeliveredAtUtc` mantém o valor da primeira

**Cenário D — produção em duas fases (flag off → flag on):**

*Fase 1 — deploy com `WebPush:AckResendEnabled` ausente/false:*

1. Deploy em produção (Railway) — migration roda no startup
2. Conferir via `railway ssh` que o schema mudou:
   `sqlite3 /data/feriados.db ".schema NotificationLogs"`
3. Inscrever seu próprio celular e seu desktop nas notificações
4. Aguardar o próximo ciclo real do job (08h BRT) com feriado na janela
5. ✅ Receber no celular; ✅ conferir `DeliveredAtUtc` preenchido no banco de
   produção; ✅ confirmar que **não** houve reenvio (flag off — mesma quantidade
   de envios de sempre)

*Fase 2 — ligar a flag (env var na Railway, sem deploy):*

6. Medir antes: `SELECT COUNT(*), COUNT(DeliveredAtUtc) FROM NotificationLogs`
   — taxa de confirmação dá noção de quantos reenvios virão
7. Ligar `WebPush:AckResendEnabled=true`
8. Teste do reenvio real: manter um navegador de teste fechado no dia do envio,
   abrir no dia seguinte após 08h → ✅ notificação chega (2ª tentativa)
9. Vigiar duplicatas reclamadas/observadas no primeiro ciclo → problema?
   desligar a flag

**Critério de lançamento:** A, B e C passando local + Fase 1 confirmada.
Fase 2 não é release — é ativação operacional, reversível em segundos, feita
quando você decidir.

---

## 9. Passos de implementação, em ordem

Cada passo deixa o projeto compilando e os testes verdes.

- [ ] **Passo 1 — Modelo, schema e migration.** `NotificationLog` ganha
  `AckToken` e `DeliveredAtUtc`; editar `AppDbContext.cs` (índice
  `(SubscriptionId, FeriadoId)` deixa de ser único; novo índice **único
  filtrado** em `AckToken` — ver seção 4.2 e o cuidado com dados existentes);
  só então `dotnet ef migrations add AckEReenvio`; rodar testes (nada quebra —
  campos novos têm default).
- [ ] **Passo 2 — Endpoint de ACK.** `AckDto` + action `Ack` no `PushController`;
  testes de integração do controller (8.2).
- [ ] **Passo 3 — Sender: token no payload.** `BuildPayload` movido para dentro
  do loop de subscriptions, com `ackToken` e `tag`; gravar token no log;
  teste `PayloadDeCadaSubscriptionTemTokenProprio`.
- [ ] **Passo 4 — Sender: regra de reenvio atrás da flag.**
  `WebPush:AckResendEnabled` lida em `LoadSettings()`; `GetAlreadySentAsync`
  evolui para a tabela de decisão da seção 5 **somente com flag ligada**;
  cinco testes de reenvio (8.2), incluindo flag desligada.
- [ ] **Passo 5 — Service worker.** Fetch do ACK + `tag` no `sw.js`.
- [ ] **Passo 6 — Teste funcional local.** Roteiro 8.3, cenários A, B, C
  (B exige flag ligada no `.env` local).
- [ ] **Passo 7 — Deploy com flag OFF.** Cenário D itens 1–5: schema migrado,
  ACK coletando, comportamento de envio idêntico ao atual.
- [ ] **Passo 8 — Observação.** 1–2 ciclos reais de feriado medindo taxa de
  confirmação (`COUNT(DeliveredAtUtc)` vs `COUNT(*)`).
- [ ] **Passo 9 — Ligar a flag na Railway.** Env var, sem deploy. Reenvio ativo.
  Problema? Desligar a flag — fim do rollback.

Plano de commits sugerido (Conventional Commits, pt-BR):

1. `feat: adiciona AckToken e DeliveredAtUtc ao NotificationLog`
2. `feat: endpoint de confirmação de entrega (ACK) no PushController`
3. `feat: payload por subscription com token de entrega`
4. `feat: reenvio de push não confirmado após 24h (atrás de feature flag)`
5. `feat: service worker confirma recebimento e colapsa reenvios`

---

## 10. Impacto e rollback

### Impacto

| Dimensão | Antes | Depois |
|---|---|---|
| Garantia de entrega | No máximo 1 tentativa (pode ser zero entrega) | Até ~5 tentativas; entrega quase garantida para quem conecta na janela |
| Privacidade | Servidor sabe que **enviou** | Servidor passa a saber **quando o usuário recebeu** (timestamp de entrega). Dado novo sobre comportamento do usuário — avaliar se merece menção em política de privacidade |
| Volume no banco | 1 linha por (usuário, feriado) | Até ~5 linhas por (usuário, feriado) no pior caso — irrelevante na escala atual |
| Tráfego | — | +1 request HTTP minúsculo por notificação recebida |
| Duplicatas | Impossíveis | Possíveis e raras (só se ACK falhar); mitigadas pela `tag` |

### Rollback

**Regra do projeto: rollback nunca pode exigir desfazer migration.**
O desenho garante isso:

- **Migration é aditiva e permanente.** Colunas novas têm default; o índice
  único antigo é substituído pela regra em código (que com a flag desligada é
  idêntica à atual). Nenhum cenário exige migration reversa.
- **Rollback do comportamento = desligar a flag.** Trocar
  `WebPush:AckResendEnabled` para `false` na Railway (env var). Sem deploy,
  sem revert de código, efeito no próximo ciclo do job. ACK e métricas de
  entrega continuam funcionando — só o reenvio para.
- **Rollback de código** (se algo além do reenvio quebrar): revert dos commits
  funciona sem tocar no banco, porque o schema extra é invisível para o código
  antigo. Único cuidado: o código antigo contava com o índice único como
  proteção extra contra duplicata em corrida — risco teórico e baixo na escala
  atual; a regra de dedup em código permanece.

---

## Glossário rápido

| Sigla/termo | Significado |
|---|---|
| ACK | Acknowledgement — confirmação de recebimento |
| APNs | Apple Push Notification service — carteiro da Apple |
| FCM | Firebase Cloud Messaging — carteiro do Google |
| GUID | Identificador único de 128 bits, gerado aleatoriamente |
| SW | Service worker — script do site que roda no navegador em segundo plano |
| TTL | Time To Live — prazo de validade da mensagem no carteiro (hoje: 24h) |
| Payload | Conteúdo da notificação (título, corpo, URL, e agora o token) |
| Dedup | Deduplicação — lógica que evita notificação repetida |
| Idempotente | Operação que pode repetir sem efeito adicional |
