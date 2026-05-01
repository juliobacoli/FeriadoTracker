# 📅 Feriado Tracker

Aplicação Fullstack para rastreio de feriados nacionais brasileiros, focada em performance, segurança e uma experiência de usuário moderna.

## 🛠️ Tecnologias e Implementação Técnica

### Backend & Dados
*   **.NET 9 (Razor Pages):** Utilização da versão mais recente da plataforma, aproveitando as melhorias de performance e a nova estrutura de renderização de páginas.
*   **Service Layer:** Arquitetura baseada em uma camada de serviço dedicada (`HolidayService`) para centralizar a lógica de busca e filtragem de feriados, mantendo as Razor Pages leves e focadas em apresentação.
*   **Entity Framework Core 9:** Gerenciamento da persistência com suporte a **Migrations automatizadas**, que garantem que o esquema do banco de dados esteja sempre atualizado na inicialização da aplicação.
*   **SQLite:** Banco de dados relacional leve e embutido, escolhido pela portabilidade e eficiência em aplicações de consulta rápida.

### Frontend & UX
*   **Vanilla JavaScript (ES6+):** Toda a lógica de interatividade — incluindo o cálculo da contagem regressiva em tempo real e a manipulação da linha do tempo — é feita com JavaScript puro, garantindo uma carga mínima de scripts e máxima performance.
*   **Modern CSS & Design Tokens:** Interface construída do zero utilizando variáveis CSS (Custom Properties) para gerenciamento de temas e tokens de design. Inclui animações otimizadas com `@keyframes` para transições de estado.
*   **Dark Mode Nativo:** Sistema de tema claro/escuro implementado via CSS e persistido no `localStorage` do navegador para manter a preferência do usuário entre sessões.
*   **Interatividade Dinâmica:** A linha do tempo permite que o usuário alterne o foco do contador para feriados futuros de forma instantânea, sem recarregar a página.

### Segurança & Performance
*   **Segurança por Design:** Implementação de uma política rigorosa de **Content Security Policy (CSP)** e headers de segurança (`X-Frame-Options`, `X-Content-Type-Options`, etc.) via middleware customizado no pipeline do ASP.NET Core.
*   **Otimização .NET 9:** Uso dos novos recursos `MapStaticAssets` e `WithStaticAssets` do .NET 9, que otimizam o roteamento e a entrega de arquivos estáticos.
*   **Localização (I18N):** Configuração global de cultura para `pt-BR`, garantindo que toda a lógica de datas e formatações siga o padrão brasileiro nativamente.
