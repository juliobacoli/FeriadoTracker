# 📅 Feriado Tracker

Aplicação Fullstack para rastreio de feriados nacionais brasileiros, focada em performance, segurança e uma experiência de usuário moderna.

## 🛠️ Tecnologias e Implementação Técnica

### Backend & Dados
*   **.NET 9 (Razor Pages):** Utilização da versão mais recente da plataforma, aproveitando as melhorias de performance e a nova estrutura de renderização de páginas.
*   **Service Layer:** Arquitetura baseada em uma camada de serviço dedicada (`HolidayService`) para centralizar a lógica de busca e filtragem de feriados, mantendo as Razor Pages leves e focadas em apresentação.
*   **Entity Framework Core 9:** Gerenciamento da persistência com suporte a **Migrations automatizadas**, que garantem que o esquema do banco de dados esteja sempre atualizado na inicialização da aplicação.
*   **SQLite:** Banco de dados relacional leve e embutido, escolhido pela portabilidade e eficiência em aplicações de consulta rápida.
*   **Web Push (VAPID):** Notificações push nativas de browser para lembrar usuários dos próximos feriados, com `BackgroundService` agendado em horário fixo (08h BRT).

### Frontend & UX
*   **Vanilla JavaScript (ES6+):** Toda a lógica de interatividade — incluindo o cálculo da contagem regressiva em tempo real e a manipulação da linha do tempo — é feita com JavaScript puro, garantindo uma carga mínima de scripts e máxima performance.
*   **Modern CSS & Design Tokens:** Interface construída do zero utilizando variáveis CSS (Custom Properties) para gerenciamento de temas e tokens de design. Inclui animações otimizadas com `@keyframes` para transições de estado.
*   **Dark Mode Nativo:** Sistema de tema claro/escuro implementado via CSS e persistido no `localStorage` do navegador para manter a preferência do usuário entre sessões.
*   **Interatividade Dinâmica:** A linha do tempo permite que o usuário alterne o foco do contador para feriados futuros de forma instantânea, sem recarregar a página.

### Segurança & Performance
*   **Segurança por Design:** Implementação de uma política rigorosa de **Content Security Policy (CSP)** com nonce + `strict-dynamic`, validação de mesmo origin nos endpoints de push e rate limit (10 req/min) para mitigar abuso.
*   **Otimização .NET 9:** Uso dos novos recursos `MapStaticAssets` e `WithStaticAssets` do .NET 9, que otimizam o roteamento e a entrega de arquivos estáticos.
*   **Localização (I18N):** Configuração global de cultura para `pt-BR` e `BrazilTimeProvider` que fixa o fuso horário em `America/Sao_Paulo` independente do host.

## 🚀 Setup local

### Pré-requisitos
*   .NET SDK 9
*   `dotnet-ef` 9 (`dotnet tool install --global dotnet-ef --version 9.0.0`)

### Variáveis de ambiente

A aplicação lê configuração de `appsettings.json` e do arquivo `.env` (em `FeriadoTracker.Web/.env`, ignorado pelo Git). Em produção, use as variáveis equivalentes do host (Railway, Docker, etc.).

| Variável | Obrigatória | Default | Descrição |
|---|---|---|---|
| `WebPush__VapidPublicKey` | sim (push) | — | Chave pública VAPID. Gere com `dotnet run --project FeriadoTracker.Web -- generate-vapid-keys`. |
| `WebPush__VapidPrivateKey` | sim (push) | — | Chave privada VAPID correspondente. **Não commitar.** |
| `WebPush__Subject` | sim (push) | `mailto:contato@juliobacoli.com.br` | Identificação do servidor para os push services (formato `mailto:` ou URL). |
| `WebPush__DaysAhead` | não | `5` | Janela em dias antes do feriado para notificar. |
| `DB_PATH` | não | `<ContentRoot>/Data/feriados.db` | Caminho do arquivo SQLite. Em hosts efêmeros, aponte para um volume persistente. |
| `DATA_PROTECTION_KEYS_PATH` | não | `<ContentRoot>/Data/keys` | Diretório onde as chaves do ASP.NET DataProtection serão persistidas. |

### Como rodar
```bash
dotnet run --project FeriadoTracker.Web
```

A aplicação escuta em `http://localhost:5228`. As migrations rodam automaticamente na inicialização.

### Gerar par de chaves VAPID
```bash
dotnet run --project FeriadoTracker.Web -- generate-vapid-keys
```
Copie a saída para o `.env` local e para as variáveis do host de produção. Substituir o par invalida todas as subscriptions existentes.

### Testes
```bash
dotnet test
```

## ⚙️ Operação

### Single-replica
O `HolidayNotificationService` (BackgroundService que envia push uma vez por dia às 08h BRT) **assume uma única instância em execução**. Múltiplas réplicas dispararão o scheduler em paralelo e enviarão notificações duplicadas. Para escalar, é necessário um lock distribuído ou um job runner externo.

### Persistência em Railway (ou hosts efêmeros)
SQLite local em containers efêmeros perde dados a cada deploy. Configure um volume persistente apontando para o caminho de `DB_PATH` (ex.: `/data/feriados.db`) e também `DATA_PROTECTION_KEYS_PATH` (ex.: `/data/keys`) para preservar as chaves de antiforgery e cookies entre deploys.

## 🔒 Endpoints

| Rota | Método | Observações |
|---|---|---|
| `/` | GET | Página principal. |
| `/health` | GET | Status do app e do `DbContext`. |
| `/api/push/vapid-public-key` | GET | Chave pública VAPID para o frontend. |
| `/api/push/subscribe` | POST | Cadastra subscription. Mesma origem + rate limit 10/min. |
| `/api/push/unsubscribe` | POST | Remove subscription. Mesma origem + rate limit 10/min. |
| `/api/dev/push/trigger` | POST | Dispara o envio diário manualmente. **Habilitado apenas em Development.** |
