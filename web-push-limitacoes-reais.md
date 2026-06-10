# Web Push — Limitações Reais

> Documento criado após incidente: notificação "Faltam 5 dias para Corpus Christi"
> entregue quando faltavam 2 dias. Objetivo: entender o problema antes de corrigir.

---

## 1. Problema observado

- Notificação recebida em 01/06/2026 às 09h36 dizendo **"Faltam 5 dias para Corpus Christi"**
- Corpus Christi = 04/06/2026
- Faltavam 3 dias na data de entrega, não 5
- UI mostrava **2 dias** (correto — countdown calculado em tempo real no browser)
- Causa imediata: mensagem foi enviada em 30/05 (quando faltavam 5 dias), entregue 2 dias depois

---

## 2. Como Web Push funciona de verdade

Fluxo:

```
App Server → Push Service → Browser
```

**3 atores. Você controla só o primeiro.**

| Ator | Quem opera | Você controla? |
|------|-----------|:-:|
| App Server | Você | Sim |
| Push Service | Mozilla (Firefox) / Google FCM (Chrome) / Apple | Não |
| Browser | Usuário | Não |

**FCM** = Firebase Cloud Messaging. Serviço Google para push em Chrome/Android.  
**Push Service** = intermediário que guarda mensagem até browser conectar.

Quando `SendNotificationAsync` retorna sucesso:
- Push service devolveu **HTTP 201** — significa "recebi e guardei"
- **Não significa** que o browser recebeu
- **Não existe ACK** (confirmação de recebimento do browser) — fire and forget

---

## 3. Glossário

| Termo | Significado |
|-------|------------|
| **TTL** | Time To Live. Segundos que push service guarda mensagem antes de descartar. Sem TTL = padrão ~4 semanas. |
| **ACK** | Acknowledgement. Confirmação de recebimento. Web Push não tem. |
| **FCM** | Firebase Cloud Messaging. Serviço de push do Google (Chrome). |
| **HTTP 201** | Código de resposta: "aceito". Push service aceitou a mensagem. Não = entregue. |
| **VAPID** | Chave criptográfica que prova que o push veio do seu servidor. `VapidPublicKey`/`VapidPrivateKey` no appsettings. |
| **Payload** | Conteúdo da notificação (título, corpo, URL). Criptografado e selado no envio. |
| **Dedup** | Deduplicação. Lógica que evita enviar a mesma notificação duas vezes. |
| **At-most-once** | Padrão de entrega: no máximo uma vez. Pode não entregar. |
| **RFC 8030** | Especificação técnica oficial do protocolo Web Push. |
| **ICU** | International Components for Unicode. Biblioteca para formatação de datas por idioma (necessária para pt-BR). |

---

## 4. Causa provável vs. causa confirmada

### Provável
Browser estava offline em 30/05 → push service guardou → browser reconectou em 01/06 → entregou.

### Não confirmado
`NotificationLog.SentAtUtc` para Corpus Christi **não foi consultado ainda.**

Se `SentAtUtc = 2026-05-30` → confirma envio em 30/05, entrega atrasada.  
Se `SentAtUtc = 2026-06-01` → outra causa, análise diferente.

### Causa alternativa possível
Push service lento ou com fila (não browser offline). Nesse caso TTL curto não resolve — apenas causa perda.

---

## 5. Problemas de design identificados

### 5.1 `NotificationLog` grava em HTTP 201, não em entrega real

```csharp
// HolidayPushSender.cs — grava log quando push service aceita
case PushSendOutcome.Success:
    db.NotificationLogs.Add(new NotificationLog { ... });
```

Push service aceitou ≠ browser recebeu. Log diz "notificado", usuário pode não ter visto nada.

### 5.2 Payload estático

Texto "Faltam 5 dias" calculado no momento do envio:

```csharp
var diffDays = feriado.Data.DayNumber - today.DayNumber;
body = NotificationTemplates.Body(diffDays, feriado.Nome);
```

Mensagem enviada em 30/05 com `diffDays=5`. Entregue em 01/06. Texto não atualiza. Não há como mudar depois que saiu do servidor.

### 5.3 Dedup por `(SubscriptionId, FeriadoId)` sem data

```sql
-- migration UmaNotificacaoPorFeriado
UNIQUE INDEX IX_NotificationLogs_SubscriptionId_FeriadoId
```

Uma tentativa por feriado por usuário. Sempre. Se a entrega falhou silenciosamente, não há retry. Usuário nunca recebe.

---

## 6. Fixes propostos e efeitos colaterais honestos

### Fix 1 — TTL curto (86400s = 1 dia)

**O que resolve:** notificação não entregue no dia é descartada. Texto nunca chega 2 dias atrasado.

**Efeito colateral:** TTL é hint ao push service, não garantia de descarte. Comportamento varia por push service (Mozilla vs. FCM).

**Risco permanente:** usuário offline no dia do envio → push service descarta → `NotificationLog` já gravado → sistema nunca tenta de novo → **usuário nunca recebe notificação daquele feriado**.

**Mudança de código necessária:**
- `VapidWebPushClient.cs`: trocar overload — sem overload que aceite `VapidDetails` + TTL + `CancellationToken` juntos. VAPID precisa ir dentro do `Dictionary<string,object>`.

```csharp
// overloads disponíveis na lib WebPush 1.0.12:
SendNotificationAsync(subscription, payload, VapidDetails, CancellationToken)   // atual
SendNotificationAsync(subscription, payload, Dictionary<string,object>, CancellationToken)  // com TTL

// para usar TTL:
var options = new Dictionary<string, object>
{
    ["vapidDetails"] = new VapidDetails(subject, publicKey, privateKey),
    ["TTL"] = 86400
};
await _client.SendNotificationAsync(subscription, payload, options, ct);
```

### Fix 2 — Mensagem com data absoluta

**O que resolve:** "Corpus Christi em 04 de junho." sempre correto, independente de quando entregue.

**Efeito colateral:** não muda mecanismo de entrega. Notificação ainda pode chegar tarde — só não vai mentir sobre quantos dias faltam.

**Mudança de código necessária:**
- `NotificationTemplates.cs`: assinatura de `Body` ganha parâmetro `DateOnly date`
- `HolidayPushSender.cs` (`BuildPayload`): passa `feriado.Data`
- `NotificationTemplatesTests.cs`: `[InlineData]` com `DateOnly` não compila em xUnit → refatorar para `[MemberData]`

### Combo Fix 1 + Fix 2

Melhor que separado. Mensagem correta + descarte rápido se não entregou.

**Risco que persiste mesmo com combo:** usuário offline no dia → perde notificação para sempre (dedup não foi alterado).

---

## 7. O que ainda não sabemos

| Incerteza | Por que importa |
|-----------|----------------|
| Causa real do delay | TTL resolve browser-offline. Não resolve push-service-lento. |
| `SentAtUtc` do Corpus Christi no DB | Confirma ou derruba hipótese principal. |
| Push service do usuário afetado | Mozilla vs. FCM têm TTL behavior diferente. |
| Ocorreu uma vez ou é sistemático? | Muda se é ajuste pontual ou problema de design. |

---

## 8. Próximo passo antes de implementar qualquer coisa

Consultar `NotificationLog` em produção:

```sql
SELECT SentDate, SentAtUtc
FROM NotificationLogs
WHERE FeriadoId = (SELECT Id FROM Feriados WHERE Nome = 'Corpus Christi')
```

Se `SentAtUtc` = 30/05 → hipótese confirmada, fixes fazem sentido.  
Se `SentAtUtc` = 01/06 → causa diferente, investigar antes de mexer.

### Como rodar

**Local** — arquivo em `FeriadoTracker.Web/Data/feriados.db`

```bash
sqlite3 "FeriadoTracker.Web/Data/feriados.db"
```

**Produção** — onde o bug aconteceu de verdade. DB_PATH vem de env var:

```bash
sqlite3 /caminho/definido/em/DB_PATH/feriados.db
```

Dentro do shell sqlite3:

```sql
SELECT nl.SentDate, nl.SentAtUtc, f.Nome
FROM NotificationLogs nl
JOIN Feriados f ON f.Id = nl.FeriadoId
WHERE f.Nome LIKE '%Corpus%';
```

> Produção é o que importa — o bug ocorreu lá. Local provavelmente não tem o log.  
> Requer acesso SSH ao servidor.
