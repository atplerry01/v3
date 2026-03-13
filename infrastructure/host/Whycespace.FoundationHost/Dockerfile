FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy all csproj files for restore layer caching
COPY src/shared/Whycespace.Shared.csproj src/shared/
COPY src/shared/contracts/Whycespace.Contracts.csproj src/shared/contracts/
COPY src/engines/Whycespace.Engines.csproj src/engines/
COPY src/runtime/Whycespace.Runtime.csproj src/runtime/
COPY src/runtime/command/Whycespace.CommandSystem.csproj src/runtime/command/
COPY src/runtime/workflow-runtime/Whycespace.WorkflowRuntime.csproj src/runtime/workflow-runtime/
COPY src/runtime/dispatcher/Whycespace.RuntimeDispatcher.csproj src/runtime/dispatcher/
COPY src/runtime/engine/Whycespace.EngineRuntime.csproj src/runtime/engine/
COPY src/runtime/partition/Whycespace.PartitionRuntime.csproj src/runtime/partition/
COPY src/runtime/engine-manifest/Whycespace.EngineManifest.csproj src/runtime/engine-manifest/
COPY src/runtime/engine-workers/Whycespace.EngineWorkerRuntime.csproj src/runtime/engine-workers/
COPY src/runtime/reliability/Whycespace.Reliability.csproj src/runtime/reliability/
COPY src/runtime/observability/Whycespace.Observability.csproj src/runtime/observability/
COPY src/runtime/projections/Whycespace.Projections.csproj src/runtime/projections/
COPY src/runtime/projection-rebuild/Whycespace.ProjectionRebuild.csproj src/runtime/projection-rebuild/
COPY src/runtime/event-fabric/Whycespace.EventFabric.csproj src/runtime/event-fabric/
COPY src/runtime/event-schema/Whycespace.EventSchema.csproj src/runtime/event-schema/
COPY src/runtime/event-idempotency/Whycespace.EventIdempotency.csproj src/runtime/event-idempotency/
COPY src/system/Whycespace.System.csproj src/system/
COPY src/system/engine-manifest/Whycespace.System.EngineManifest.csproj src/system/engine-manifest/
COPY src/system/upstream/WhyceID/Whycespace.System.WhyceID.csproj src/system/upstream/WhyceID/
COPY src/domain/Whycespace.Domain.csproj src/domain/
COPY src/domain/clusters/Whycespace.ClusterDomain.csproj src/domain/clusters/
COPY src/engines/T0U/WhyceID/Whycespace.Engines.T0U.WhyceID.csproj src/engines/T0U/WhyceID/
COPY src/engines/T0U/Governance/Whycespace.Engines.T0U.Governance.csproj src/engines/T0U/Governance/
COPY src/engines/T0U/WhycePolicy/Whycespace.Engines.T0U.WhycePolicy.csproj src/engines/T0U/WhycePolicy/
COPY src/engines/T0U/WhyceChain/Whycespace.Engines.T0U.WhyceChain.csproj src/engines/T0U/WhyceChain/
COPY src/engines/T1M/WSS/Whycespace.Engines.T1M.WSS.csproj src/engines/T1M/WSS/
COPY infrastructure/host/Whycespace.FoundationHost/Whycespace.FoundationHost.csproj infrastructure/host/Whycespace.FoundationHost/
RUN dotnet restore infrastructure/host/Whycespace.FoundationHost/Whycespace.FoundationHost.csproj

COPY src/ src/
COPY infrastructure/host/Whycespace.FoundationHost/ infrastructure/host/Whycespace.FoundationHost/
RUN dotnet publish infrastructure/host/Whycespace.FoundationHost/Whycespace.FoundationHost.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Whycespace.FoundationHost.dll"]
