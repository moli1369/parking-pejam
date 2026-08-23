# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/ParkingPejam.Web/ParkingPejam.Web.csproj
RUN dotnet publish src/ParkingPejam.Web/ParkingPejam.Web.csproj -c Release -o /app/publish /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0 \
    COMPlus_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

RUN addgroup -S -g 1654 parking && adduser -S -D -H -u 1654 -G parking parking \
    && mkdir -p /app/Data /app/license /run/secrets \
    && chown -R 1654:1654 /app

COPY --from=build --chown=1654:1654 /app/publish .
COPY --chown=1654:1654 deploy/container-entrypoint.sh /usr/local/bin/parking-pejam-entrypoint
RUN chmod 0555 /usr/local/bin/parking-pejam-entrypoint

USER 1654:1654
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=5 \
  CMD wget -q -O - http://127.0.0.1:8080/health/live >/dev/null || exit 1

ENTRYPOINT ["/usr/local/bin/parking-pejam-entrypoint"]
