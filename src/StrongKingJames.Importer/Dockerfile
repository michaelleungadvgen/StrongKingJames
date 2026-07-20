# syntax=docker/dockerfile:1
#
# Multi-stage build for the StrongKingJames importer console app.
# Build context MUST be the repository root (docker build -f src/StrongKingJames.Importer/Dockerfile .)
# so that Directory.Build.props, global.json, and referenced projects are available.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Solution-wide files that influence restore/build for every project.
COPY global.json Directory.Build.props ./

# Copy csproj files first to maximize NuGet restore layer caching.
COPY src/StrongKingJames.Core/StrongKingJames.Core.csproj src/StrongKingJames.Core/
COPY src/StrongKingJames.Data/StrongKingJames.Data.csproj src/StrongKingJames.Data/
COPY src/StrongKingJames.ServiceDefaults/StrongKingJames.ServiceDefaults.csproj src/StrongKingJames.ServiceDefaults/
COPY src/StrongKingJames.Importer/StrongKingJames.Importer.csproj src/StrongKingJames.Importer/

RUN dotnet restore src/StrongKingJames.Importer/StrongKingJames.Importer.csproj

# Copy the remaining source and publish.
COPY src/ src/
RUN dotnet publish src/StrongKingJames.Importer/StrongKingJames.Importer.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./

# Default data paths point at the bind-mounted /data directory (see docker-compose.yml).
# The kjs JSON source carries per-word Strong's numbers for both OT (H####) and NT (G####).
# Override with --format osis --osis ... --hebrew ... --greek ... to use OSIS sources instead.
ENTRYPOINT ["dotnet", "StrongKingJames.Importer.dll"]
CMD ["--format", "kjs", "--kjs-bible", "/data/kjv_pure.json", "--kjs-strongs", "/data/strong_pure.json", "--kjs-dict", "/data/strong_dict.json"]
