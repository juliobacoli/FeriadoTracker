# ---------- build ----------
# Alpine + linux-musl-x64: o allocator do musl mantém RSS bem menor que o glibc
# para .NET, e a imagem base não traz ICU (a app roda em InvariantGlobalization).
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY FeriadoTracker.Web/FeriadoTracker.Web.csproj ./FeriadoTracker.Web/
RUN dotnet restore ./FeriadoTracker.Web/FeriadoTracker.Web.csproj -r linux-musl-x64

COPY . ./
# ReadyToRun: código pré-compilado e mapeado do arquivo (páginas limpas, evictáveis)
# no lugar de JIT em memória privada suja. Reduz RSS e o tempo de startup.
RUN dotnet publish ./FeriadoTracker.Web/FeriadoTracker.Web.csproj \
    -c Release -o /app/out \
    -r linux-musl-x64 --self-contained false \
    -p:PublishReadyToRun=true

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
# tzdata é a única dependência de globalização que sobra (America/Sao_Paulo).
RUN apk add --no-cache tzdata
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV TZ=America/Sao_Paulo

# GC workstation e não concorrente: remove a thread e os segmentos do background GC.
ENV DOTNET_gcServer=0
ENV DOTNET_gcConcurrent=0
# Compacta mais agressivamente e devolve páginas ao SO (escala 0-9).
ENV DOTNET_GCConserveMemory=5
# Budget de gen0 de 4 MiB (valor em hex) no lugar do default derivado do cache da CPU.
ENV DOTNET_GCgen0size=400000
# Sem servidor de diagnóstico IPC: remove uma thread e mapeamentos do processo.
ENV DOTNET_EnableDiagnostics=0
# Sem instrumentação de PGO dinâmico.
ENV DOTNET_TieredPGO=0

ENTRYPOINT ["dotnet", "FeriadoTracker.Web.dll"]
