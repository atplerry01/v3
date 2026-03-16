using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Whycespace.Engines.T3I.Clusters.Mobility.Taxi;
using Whycespace.FoundationHost.Workers;
using Whycespace.Runtime.EngineManifest.Loader;
using Whycespace.Runtime.Dispatcher;
using Whycespace.EventFabricRuntime.Bus;
using Whycespace.Runtime.Observability;
using Whycespace.Runtime.Persistence;
using Whycespace.Systems.Midstream.WhyceAtlas.Projections;
using Whycespace.Systems.Downstream.Mobility.Projections;
using Whycespace.Systems.Downstream.Property.Projections;
using Whycespace.ProjectionRuntime.Projections.Queries;
using Whycespace.ProjectionRuntime.Projections.Registry;
using Whycespace.ProjectionRuntime.Storage;
using Whycespace.EventIdempotency.Guard;
using Whycespace.EventIdempotency.Registry;
using Whycespace.EngineRuntime.Registry;
using Whycespace.Runtime.Reliability;
using Whycespace.WorkflowRuntime;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Runtime;
using Whycespace.Systems.Midstream.WSS.Kafka;
using Whycespace.Systems.Midstream.WSS.Mapping;
using Whycespace.Systems.Midstream.WSS.Orchestration;
using Whycespace.Systems.Midstream.WSS.Workflows;
using Whycespace.CommandSystem.Catalog;
using Whycespace.CommandSystem.Idempotency;
using Whycespace.CommandSystem.Models;
using Whycespace.CommandSystem.Routing;
using Whycespace.CommandSystem.Validation;
using CmdDispatcher = Whycespace.CommandSystem.Dispatcher.CommandDispatcher;
using Whycespace.WorkflowRuntime.Executor;
using Whycespace.WorkflowRuntime.Registry;
using WfRuntime = Whycespace.WorkflowRuntime.Runtime.WorkflowRuntime;
using Whycespace.EngineRuntime.Executor;
using Whycespace.EngineRuntime.Invocation;
using Whycespace.EngineRuntime.Resolver;
using EngineReg = Whycespace.EngineRuntime.Registry.EngineRegistry;
using IEngineReg = Whycespace.EngineRuntime.Registry.IEngineRegistry;
using Whycespace.EngineRuntime.Registry;
using Whycespace.RuntimeDispatcher.Resolver;
using Whycespace.RuntimeDispatcher.Pipeline;
using RtDispatcher = Whycespace.RuntimeDispatcher.Dispatcher.RuntimeDispatcher;
using IRtDispatcher = Whycespace.RuntimeDispatcher.Dispatcher.IRuntimeDispatcher;
using Whycespace.PartitionRuntime.Resolver;
using Whycespace.PartitionRuntime.Router;
using Whycespace.PartitionRuntime.Worker;
using Whycespace.PartitionRuntime.Dispatcher;
using Whycespace.EngineWorkerRuntime.Queue;
using Whycespace.EngineWorkerRuntime.Pool;
using Whycespace.EngineWorkerRuntime.Supervisor;
using Whycespace.Observability.Metrics;
using Whycespace.Observability.Tracing;
using Whycespace.Observability.Diagnostics;
using Whycespace.Observability.Health;
using Whycespace.ProjectionRebuild.Reader;
using Whycespace.ProjectionRebuild.Reset;
using Whycespace.ProjectionRebuild.Checkpoints;
using Whycespace.ProjectionRebuild.Rebuild;
using Whycespace.ProjectionRebuild.Controller;

// Serilog
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Whycespace.FoundationHost")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Whycespace Foundation Host");

    var builder = WebApplication.CreateSlimBuilder(args);
    builder.Host.UseSerilog();

    // OpenTelemetry
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("Whycespace.FoundationHost"))
        .WithTracing(t => t
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Whycespace.Runtime")
            .AddSource("Whycespace.Engines"));

    // Configuration
    var postgresConn = builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Database=whycespace;Username=whyce;Password=whyce";
    var kafkaBrokers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

    // Engine Runtime — registry, manifest loader, resolver, invocation manager, executor
    var engineRegistry = new EngineReg();

    var engineManifestLoader = new EngineManifestLoader(engineRegistry);
    engineManifestLoader.LoadFromAssembly(typeof(DriverMatchingEngine).Assembly);

    builder.Services.AddSingleton<IEngineReg>(engineRegistry);
    builder.Services.AddSingleton(engineManifestLoader);

    var engineResolver = new EngineResolver(engineRegistry);
    builder.Services.AddSingleton(engineResolver);

    var engineInvocationManager = new EngineInvocationManager();
    builder.Services.AddSingleton(engineInvocationManager);

    var workflowStepEngineExecutor = new WorkflowStepEngineExecutor(engineResolver, engineInvocationManager);
    builder.Services.AddSingleton(workflowStepEngineExecutor);

    // Legacy engine dispatcher (for backward compatibility)
    var legacyEngineRegistry = new Whycespace.EngineRuntime.Registry.EngineRegistry();
    foreach (var name in engineRegistry.ListEngines())
        legacyEngineRegistry.Register(engineRegistry.Resolve(name));
    builder.Services.AddSingleton(legacyEngineRegistry);

    var engineDispatcher = new Whycespace.Runtime.Dispatcher.RuntimeDispatcher(legacyEngineRegistry);
    builder.Services.AddSingleton(engineDispatcher);
    builder.Services.AddSingleton<IEngineRuntimeDispatcher>(engineDispatcher);

    var workflowStateStore = new WorkflowStateStore();
    builder.Services.AddSingleton(workflowStateStore);

    var orchestrator = new WorkflowOrchestrator(engineDispatcher, workflowStateStore);
    builder.Services.AddSingleton(orchestrator);
    builder.Services.AddSingleton<IWorkflowOrchestrator>(orchestrator);

    var eventBus = new EventBus();
    builder.Services.AddSingleton(eventBus);
    builder.Services.AddSingleton<IEventBus>(eventBus);

    // Reliability
    builder.Services.AddSingleton(new IdempotencyRegistry());
    builder.Services.AddSingleton(new RetryPolicyEngine());
    builder.Services.AddSingleton(new TimeoutManager());
    builder.Services.AddSingleton(new SagaCoordinator());
    builder.Services.AddSingleton(new DeadLetterQueue());

    // Observability
    builder.Services.AddSingleton(new RuntimeObserver());
    var metricsCollector = new MetricsCollector();
    var traceManager = new TraceManager();
    var workflowDiagnostics = new WorkflowDiagnosticsService();
    var engineTelemetry = new EngineTelemetryService();
    var healthCheckService = new HealthCheckService();
    builder.Services.AddSingleton(metricsCollector);
    builder.Services.AddSingleton(traceManager);
    builder.Services.AddSingleton(workflowDiagnostics);
    builder.Services.AddSingleton(engineTelemetry);
    builder.Services.AddSingleton(healthCheckService);

    // Projection System (CQRS)
    var projectionStore = new RedisProjectionStore();
    builder.Services.AddSingleton<IProjectionStore>(projectionStore);

    var projectionRegistry = new Whycespace.ProjectionRuntime.Projections.Registry.ProjectionRegistry();
    projectionRegistry.Register(new DriverLocationProjection(projectionStore));
    projectionRegistry.Register(new RideStatusProjection(projectionStore));
    projectionRegistry.Register(new PropertyListingProjection(projectionStore));
    projectionRegistry.Register(new VaultBalanceProjection(projectionStore));
    projectionRegistry.Register(new RevenueProjection(projectionStore));
    builder.Services.AddSingleton<IProjectionRegistry>(projectionRegistry);

    var deduplicationRegistry = new EventDeduplicationRegistry();
    var processingGuard = new EventProcessingGuard(deduplicationRegistry);
    builder.Services.AddSingleton(processingGuard);

    var projectionQueryService = new ProjectionQueryService(projectionStore);
    builder.Services.AddSingleton(projectionQueryService);

    // Projection Rebuild Engine
    var eventLogReader = new EventLogReader();
    builder.Services.AddSingleton(eventLogReader);

    var projectionResetService = new ProjectionResetService(projectionStore, projectionRegistry);
    builder.Services.AddSingleton(projectionResetService);

    var projectionCheckpointStore = new ProjectionCheckpointStore();
    builder.Services.AddSingleton(projectionCheckpointStore);

    var projectionRebuildEngine = new ProjectionRebuildEngine(eventLogReader, projectionRegistry, projectionResetService, projectionCheckpointStore);
    builder.Services.AddSingleton(projectionRebuildEngine);

    var projectionReplayController = new ProjectionReplayController(projectionRebuildEngine, projectionResetService, projectionRegistry);
    builder.Services.AddSingleton(projectionReplayController);

    // Persistence
    builder.Services.AddSingleton(new PostgresEventStore(postgresConn));
    builder.Services.AddSingleton(new ProjectionStore(postgresConn));
    builder.Services.AddSingleton(new WorkflowStateRepository(postgresConn));

    // Workflow Mapper
    var workflowMapper = new WorkflowMapper();
    workflowMapper.Register(new RideRequestWorkflow());
    workflowMapper.Register(new PropertyListingWorkflow());
    workflowMapper.Register(new EconomicLifecycleWorkflow());
    builder.Services.AddSingleton(workflowMapper);

    // Workflow Runtime
    var workflowRegistry = new WorkflowRegistry();
    var rideGraph = new RideRequestWorkflow().BuildGraph();
    var propertyGraph = new PropertyListingWorkflow().BuildGraph();
    var economicGraph = new EconomicLifecycleWorkflow().BuildGraph();
    workflowRegistry.Register(rideGraph);
    workflowRegistry.Register(propertyGraph);
    workflowRegistry.Register(economicGraph);
    builder.Services.AddSingleton<IWorkflowRegistry>(workflowRegistry);

    var workflowExecutor = new WorkflowExecutor(workflowStepEngineExecutor.ExecuteStepAsync);
    builder.Services.AddSingleton<IWorkflowExecutor>(workflowExecutor);

    var workflowRuntime = new WfRuntime(workflowRegistry, workflowExecutor);
    builder.Services.AddSingleton(workflowRuntime);

    // WSS Orchestrator
    var wssOrchestrator = new WSSOrchestrator(workflowMapper, orchestrator, workflowRuntime);
    builder.Services.AddSingleton(wssOrchestrator);

    // Command System
    var commandCatalog = new CommandCatalog();
    commandCatalog.Register("RequestRideCommand", typeof(Whycespace.Domain.Application.Commands.RequestRideCommand));
    commandCatalog.Register("ListPropertyCommand", typeof(Whycespace.Domain.Application.Commands.ListPropertyCommand));
    commandCatalog.Register("AllocateCapitalCommand", typeof(Whycespace.Domain.Application.Commands.AllocateCapitalCommand));
    builder.Services.AddSingleton<ICommandCatalog>(commandCatalog);

    var commandValidator = new CommandValidator();
    builder.Services.AddSingleton<ICommandValidator>(commandValidator);

    var commandIdempotency = new InMemoryIdempotencyRegistry();
    builder.Services.AddSingleton<IIdempotencyRegistry>(commandIdempotency);

    var commandRouter = new CommandRouter();
    commandRouter.MapCommand("RequestRideCommand", "RideRequestWorkflow");
    commandRouter.MapCommand("ListPropertyCommand", "PropertyListingWorkflow");
    commandRouter.MapCommand("AllocateCapitalCommand", "EconomicLifecycleWorkflow");
    builder.Services.AddSingleton<ICommandRouter>(commandRouter);

    // Partition Runtime
    var partitionKeyResolver = new PartitionKeyResolver();
    builder.Services.AddSingleton<IPartitionKeyResolver>(partitionKeyResolver);

    var partitionRouter = new PartitionRouter(16);
    builder.Services.AddSingleton<IPartitionRouter>(partitionRouter);

    var partitionWorkerPool = new PartitionWorkerPool(16, workflowRuntime);
    builder.Services.AddSingleton(partitionWorkerPool);

    var workflowPartitionDispatcher = new WorkflowPartitionDispatcher(partitionKeyResolver, partitionRouter, partitionWorkerPool);
    builder.Services.AddSingleton(workflowPartitionDispatcher);

    // Engine Worker Runtime
    var engineQueueRegistry = new PartitionEngineQueueRegistry(16);
    builder.Services.AddSingleton(engineQueueRegistry);

    var engineWorkerPool = new PartitionEngineWorkerPool(16, 4, engineResolver, engineQueueRegistry);
    builder.Services.AddSingleton(engineWorkerPool);

    var engineWorkerSupervisor = new EngineWorkerSupervisor(engineWorkerPool);
    builder.Services.AddSingleton(engineWorkerSupervisor);

    // Runtime Dispatcher
    var workflowResolver = new WorkflowResolver();
    builder.Services.AddSingleton<IWorkflowResolver>(workflowResolver);

    var runtimeDispatcher = new RtDispatcher(commandValidator, commandIdempotency, workflowResolver, workflowRuntime, workflowPartitionDispatcher);
    builder.Services.AddSingleton(runtimeDispatcher);
    builder.Services.AddSingleton<IRtDispatcher>(runtimeDispatcher);

    var executionPipeline = new ExecutionPipeline(commandValidator, commandIdempotency, workflowResolver, workflowRuntime, workflowPartitionDispatcher);
    builder.Services.AddSingleton(executionPipeline);

    // Command Dispatcher — delegates to RuntimeDispatcher
    var cmdDispatcher = new CmdDispatcher(runtimeDispatcher.DispatchAsync);
    builder.Services.AddSingleton(cmdDispatcher);

    // Kafka Publisher
    builder.Services.AddSingleton(new KafkaEventPublisher(eventBus, kafkaBrokers));

    // Background workers
    builder.Services.AddHostedService<WorkflowWorker>();
    builder.Services.AddHostedService<ProjectionWorker>();
    builder.Services.AddHostedService<KafkaConsumerWorker>();

    // Minimal web host for health endpoint
    builder.WebHost.UseUrls("http://0.0.0.0:8080");

    var host = builder.Build();

    host.MapGet("/dev/system/runtime", () => Results.Json(new
    {
        runtime = "foundation-host",
        status = "running",
        engines = engineRegistry.ListEngines().Count,
        timestamp = DateTimeOffset.UtcNow
    }));

    host.MapGet("/dev/contracts", () => Results.Json(new
    {
        contracts = new[]
        {
            "ICommand",
            "IEvent",
            "IEngine",
            "WorkflowGraph",
            "WorkflowContext"
        }
    }));

    host.MapGet("/dev/commands", () => Results.Json(new
    {
        commands = commandCatalog.GetRegisteredCommands(),
        routes = commandRouter.GetRoutes(),
        timestamp = DateTimeOffset.UtcNow
    }));

    host.MapPost("/dev/commands/dispatch", async (CommandEnvelope envelope) =>
    {
        try
        {
            var result = await cmdDispatcher.DispatchAsync(envelope);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    });

    host.MapGet("/dev/workflows", () => Results.Json(new
    {
        workflows = workflowRegistry.GetRegisteredWorkflows(),
        timestamp = DateTimeOffset.UtcNow
    }));

    host.MapPost("/dev/workflows/run", async (WorkflowExecutionRequest request) =>
    {
        try
        {
            var result = await workflowRuntime.ExecuteAsync(request);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    });

    // Runtime Dispatcher debug endpoints
    host.MapGet("/dev/runtime/dispatcher", () => Results.Json(new
    {
        dispatcher = "active"
    }));

    host.MapPost("/dev/runtime/dispatch", async (CommandEnvelope envelope) =>
    {
        try
        {
            var result = await runtimeDispatcher.DispatchAsync(envelope, CancellationToken.None);
            return Results.Ok(new
            {
                success = result.Success,
                workflowName = workflowResolver.ResolveWorkflow(envelope.CommandType),
                errorMessage = result.ErrorMessage,
                output = result.Output
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    });

    // Partition Runtime debug endpoints
    host.MapGet("/dev/partitions", () => Results.Json(new
    {
        partitionCount = partitionRouter.PartitionCount
    }));

    host.MapGet("/dev/partitions/workers", () => Results.Json(new
    {
        workers = partitionWorkerPool.GetActivePartitions().Select(id => new { partitionId = id })
    }));

    // Engine Manifest debug endpoints
    host.MapGet("/dev/engines/manifests", () => Results.Json(new
    {
        engines = engineManifestLoader.GetManifests().Select(m => new
        {
            name = m.EngineName,
            tier = m.Tier.ToString(),
            kind = m.Kind.ToString()
        })
    }));

    host.MapGet("/dev/engines/registry", () => Results.Json(new
    {
        engines = engineManifestLoader.GetDescriptors().Select(d => new
        {
            name = d.Metadata.EngineName,
            tier = d.Metadata.Tier.ToString(),
            kind = d.Metadata.Kind.ToString(),
            inputContract = d.Metadata.InputContract,
            outputEvents = d.Metadata.OutputEvents
        })
    }));

    // Engine Runtime debug endpoints
    host.MapGet("/dev/engines", () => Results.Json(new
    {
        engines = engineRegistry.ListEngines()
    }));

    host.MapPost("/dev/engines/invoke", async (EngineInvocationEnvelope envelope) =>
    {
        try
        {
            var engine = engineResolver.Resolve(envelope.EngineName);
            var context = new EngineContext(
                envelope.InvocationId,
                envelope.WorkflowId,
                envelope.WorkflowStep,
                envelope.PartitionKey,
                envelope.Context);
            var result = await engineInvocationManager.InvokeAsync(engine, context);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    });

    // T2E Execution Engines debug endpoint
    host.MapGet("/dev/engines/t2e", () => Results.Json(new
    {
        executionEngines = engineManifestLoader.GetManifests()
            .Where(m => m.Tier.ToString() == "T2E")
            .Select(m => m.EngineName)
    }));

    // T3I Decision Engines debug endpoint
    host.MapGet("/dev/engines/t3i", () => Results.Json(new
    {
        decisionEngines = engineManifestLoader.GetManifests()
            .Where(m => m.Tier.ToString() == "T3I")
            .Select(m => m.EngineName)
    }));

    // Engine Worker Runtime debug endpoints
    host.MapGet("/dev/engine-workers", () => Results.Json(new
    {
        workersPerPartition = engineWorkerPool.WorkersPerPartition,
        totalWorkers = engineWorkerPool.TotalWorkerCount
    }));

    host.MapGet("/dev/engine-workers/status", () => Results.Json(new
    {
        workers = engineWorkerPool.Workers.Select(kv => new
        {
            partition = kv.Key,
            workers = kv.Value.Count
        })
    }));

    host.MapGet("/dev/engine-workers/queues", () => Results.Json(new
    {
        queues = Enumerable.Range(0, engineQueueRegistry.PartitionCount).Select(p => new
        {
            partition = p,
            pending = engineQueueRegistry.GetPendingCount(p)
        })
    }));

    // Observability debug endpoints
    host.MapGet("/dev/metrics", () => Results.Json(metricsCollector.GetAll()));

    host.MapGet("/dev/traces", () => Results.Json(new
    {
        activeTraces = traceManager.GetActiveTraceCount()
    }));

    host.MapGet("/dev/health", () =>
    {
        var statuses = healthCheckService.CheckAll();
        return Results.Json(statuses.ToDictionary(s => s.Component, s => s.Status));
    });

    // Projection debug endpoints
    host.MapGet("/dev/projections", () => Results.Json(new
    {
        projections = projectionRegistry.GetAll().Select(p => p.Name)
    }));

    host.MapGet("/dev/projections/query", async (string? key) =>
    {
        if (string.IsNullOrWhiteSpace(key))
            return Results.BadRequest(new { error = "key parameter is required" });

        var value = await projectionQueryService.GetAsync(key);
        return value is not null
            ? Results.Ok(new { key, value })
            : Results.NotFound(new { key, error = "not found" });
    });

    // Projection Rebuild debug endpoints
    host.MapPost("/dev/projections/rebuild", async () =>
    {
        await projectionReplayController.RebuildAllAsync();
        return Results.Ok(new { status = "rebuild complete", processedEvents = projectionReplayController.GetStatus().ProcessedEvents });
    });

    host.MapPost("/dev/projections/rebuild/{projection}", async (string projection) =>
    {
        await projectionReplayController.RebuildProjectionAsync(projection);
        return Results.Ok(new { projection, status = "rebuild complete", processedEvents = projectionReplayController.GetStatus().ProcessedEvents });
    });

    host.MapGet("/dev/projections/checkpoints", () => Results.Json(new
    {
        checkpoints = projectionCheckpointStore.GetAll().Select(c => new
        {
            projection = c.ProjectionName,
            lastEventId = c.LastProcessedEventId,
            timestamp = c.Timestamp
        })
    }));

    // Start engine worker supervisor
    engineWorkerSupervisor.Start(CancellationToken.None);

    Log.Information("Foundation Host initialized with {EngineCount} engines", engineRegistry.ListEngines().Count);

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Foundation Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
