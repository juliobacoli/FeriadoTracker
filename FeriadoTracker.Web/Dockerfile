FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG TARGETARCH
WORKDIR /src

COPY FeriadoTracker.Web/FeriadoTracker.Web.csproj ./FeriadoTracker.Web/
RUN RID="linux-musl-$([ "$TARGETARCH" = arm64 ] && echo arm64 || echo x64)" && \
    dotnet restore ./FeriadoTracker.Web/FeriadoTracker.Web.csproj -r "$RID"

COPY . ./
RUN RID="linux-musl-$([ "$TARGETARCH" = arm64 ] && echo arm64 || echo x64)" && \
    dotnet publish ./FeriadoTracker.Web/FeriadoTracker.Web.csproj \
    -c Release -o /app/out \
    -r "$RID" --self-contained false \
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
