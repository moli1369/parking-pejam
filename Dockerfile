# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/ParkingPejam.Web/ParkingPejam.Web.csproj
RUN dotnet publish src/ParkingPejam.Web/ParkingPejam.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0 \
    COMPlus_EnableDiagnostics=0

RUN mkdir -p /app/Data /app/Data/keys /app/license \
    && chown -R 1654:1654 /app

COPY --from=build --chown=1654:1654 /app/publish .

USER 1654:1654
EXPOSE 8080

ENTRYPOINT ["dotnet","ParkingPejam.Web.dll"]
