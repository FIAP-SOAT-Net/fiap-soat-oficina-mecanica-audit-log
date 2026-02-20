FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080

# Install dependencies required by New Relic native profiler
RUN apk add --no-cache libgcc libstdc++

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
COPY . .

RUN dotnet build "src/Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker/Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker/Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

# Enable New Relic profiler (agent files are included via NuGet package)
ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
    CORECLR_NEWRELIC_HOME=/app/newrelic \
    CORECLR_PROFILER_PATH=/app/newrelic/libNewRelicProfiler.so \
    NEW_RELIC_DISTRIBUTED_TRACING_ENABLED=true \
    NEW_RELIC_LOG_LEVEL=info

COPY --from=publish /app/publish .

# Create directory for Data Protection keys
RUN mkdir -p /app/keys && chmod 755 /app/keys

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Fiap.Soat.SmartMechanicalWorkshop.AuditLog.Worker.dll"]
