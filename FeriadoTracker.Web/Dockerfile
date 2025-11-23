FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# aqui precisa apontar para a subpasta:
COPY FeriadoTracker.Web/FeriadoTracker.Web.csproj ./FeriadoTracker.Web/
RUN dotnet restore ./FeriadoTracker.Web/FeriadoTracker.Web.csproj

COPY . ./
RUN dotnet publish ./FeriadoTracker.Web/FeriadoTracker.Web.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "FeriadoTracker.Web.dll"]