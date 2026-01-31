# ============================
# Base (runtime)
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

USER 0

WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# ============================
# Build
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILD_CONFIGURATION=Release

# Build metadata
ARG APP_VERSION=0.0.0
ARG GIT_SHA=unknown
ARG BUILD_DATE=unknown

# Apply env vraiblaes
ENV APP_VERSION=$APP_VERSION
ENV GIT_SHA=$GIT_SHA
ENV BUILD_DATE=$BUILD_DATE

WORKDIR /src

COPY ["MyFicDB.Web/MyFicDB.Web.csproj", "MyFicDB.Web/"]
COPY ["MyFicDB.Core/MyFicDB.Core.csproj", "MyFicDB.Core/"]
COPY ["MyFicDB.Exporter/MyFicDB.Exporter.csproj", "MyFicDB.Exporter/"]

RUN dotnet restore "./MyFicDB.Web/MyFicDB.Web.csproj"

COPY . .
WORKDIR "/src/MyFicDB.Web"

RUN dotnet build "./MyFicDB.Web.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    /p:Version=$APP_VERSION \
    /p:InformationalVersion="$APP_VERSION+$GIT_SHA" \
    /p:SourceRevisionId=$GIT_SHA

# ============================
# Publish
# ============================
FROM build AS publish

ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "./MyFicDB.Web.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:Version=$APP_VERSION \
    /p:InformationalVersion="$APP_VERSION+$GIT_SHA" \
    /p:SourceRevisionId=$GIT_SHA

# ============================
# Final (runtime)
# ============================
FROM base AS final
WORKDIR /app

# OCI image labels
ARG APP_VERSION=0.0.0
ARG GIT_SHA=unknown
ARG BUILD_DATE=unknown

# Make build metadata available at runtime
ENV APP_VERSION=$APP_VERSION
ENV GIT_SHA=$GIT_SHA
ENV BUILD_DATE=$BUILD_DATE

LABEL org.opencontainers.image.title="MyFicDB" \
      org.opencontainers.image.version=$APP_VERSION \
      org.opencontainers.image.revision=$GIT_SHA \
      org.opencontainers.image.created=$BUILD_DATE

# Fix garbage collection sucking up all the resources
# see https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector
ENV DOTNET_gcServer=0

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyFicDB.Web.dll"]