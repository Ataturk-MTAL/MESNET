# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Solution ve proje dosyalarını önce kopyala (Docker layer cache)
COPY MESNET.slnx Directory.Build.props Directory.Packages.props global.json ./
COPY src/ ./src/

WORKDIR /src/src/MESNET.Presentation
RUN dotnet publish -c Release -o /app/publish --no-self-contained

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# /alive — canlılık ucu, HER ortamda map edilir. /health DEĞİL: o uç yalnız Development'ta
# açıktır ve bu imaj Production ile koşar; /health'e giden bir HEALTHCHECK 404 alır ve konteyner
# kalıcı olarak unhealthy görünür (bkz. ServiceDefaults.MapDefaultEndpoints).
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget -q --spider http://localhost:8080/alive || exit 1

ENTRYPOINT ["dotnet", "MESNET.Presentation.dll"]
