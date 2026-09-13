FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY FeriadoTracker.Web/FeriadoTracker.Web.csproj ./FeriadoTracker.Web/
RUN dotnet restore ./FeriadoTracker.Web/FeriadoTracker.Web.csproj -r linux-musl-x64

COPY . ./
RUN dotnet publish ./FeriadoTracker.Web/FeriadoTracker.Web.csproj \
    -c Release -o /app/out \
    -r linux-musl-x64 --self-contained false \
    -p:PublishReadyToRun=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
RUN apk add --no-cache tzdata
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV TZ=America/Sao_Paulo

ENV DOTNET_gcServer=0
ENV DOTNET_gcConcurrent=0
ENV DOTNET_GCConserveMemory=5
ENV DOTNET_GCgen0size=400000
ENV DOTNET_EnableDiagnostics=0
ENV DOTNET_TieredPGO=0

ENTRYPOINT ["dotnet", "FeriadoTracker.Web.dll"]
