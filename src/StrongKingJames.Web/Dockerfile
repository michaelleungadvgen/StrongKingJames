# syntax=docker/dockerfile:1
#
# Multi-stage build for the StrongKingJames Blazor Server web app.
# Build context MUST be the repository root (docker build -f src/StrongKingJames.Web/Dockerfile .)
# so that Directory.Build.props, global.json, and referenced projects are available.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Solution-wide files that influence restore/build for every project.
COPY global.json Directory.Build.props ./

# Copy csproj files first to maximize NuGet restore layer caching.
COPY src/StrongKingJames.Core/StrongKingJames.Core.csproj src/StrongKingJames.Core/
COPY src/StrongKingJames.Data/StrongKingJames.Data.csproj src/StrongKingJames.Data/
COPY src/StrongKingJames.ServiceDefaults/StrongKingJames.ServiceDefaults.csproj src/StrongKingJames.ServiceDefaults/
COPY src/StrongKingJames.Web/StrongKingJames.Web.csproj src/StrongKingJames.Web/

RUN dotnet restore src/StrongKingJames.Web/StrongKingJames.Web.csproj

# Copy the remaining source and publish.
COPY src/ src/
RUN dotnet publish src/StrongKingJames.Web/StrongKingJames.Web.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./

# Listen on all interfaces so the container is reachable via the published port.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "StrongKingJames.Web.dll"]
