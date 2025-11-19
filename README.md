# FeriadoTracker

# 📅 Feriado Tracker (.NET 9)

Aplicação Fullstack para rastrear o próximo feriado nacional com contagem regressiva em tempo real.

## 🚀 Tecnologias
- **Backend:** .NET 9 (Razor Pages)
- **Database:** SQLite + Entity Framework Core
- **Frontend:** Alpine.js (Reatividade) + Bootstrap 5
- **Arquitetura:** Service Layer & Repository Pattern

## ⚙️ Como Rodar
1. Clone o repositório.
2. Restaure os pacotes e banco de dados:
   ```bash
   dotnet tool update --global dotnet-ef
   dotnet ef database update --project FeriadoTracker.Web