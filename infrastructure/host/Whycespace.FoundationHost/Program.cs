using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Whycespace.Engines.T0U_Constitutional;
using Whycespace.Engines.T1M_Orchestration;
using Whycespace.Engines.T2E_Execution;
using Whycespace.Engines.T3I_Intelligence;
using Whycespace.Engines.T4A_Access;
using Whycespace.FoundationHost.Workers;
using Whycespace.Runtime.Dispatcher;
using Whycespace.Runtime.Events;
using Whycespace.Runtime.Observability;
using Whycespace.Runtime.Partitions;
using Whycespace.Runtime.Persistence;
using Whycespace.Runtime.Projections;
using Whycespace.Runtime.Registry;
using Whycespace.Runtime.Reliability;
using Whycespace.Runtime.Workflow;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Runtime;
using Whycespace.Shared.Projections;
using Whycespace.System.Midstream.WSS.Kafka;
using Whycespace.System.Midstream.WSS.Mapping;
using Whycespace.System.Midstream.WSS.Orchestration;
using Whycespace.System.Midstream.WSS.Workflows;
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

    // Engine Runtime — registry, resolver, invocation manager, executor
    var engineRegistry = new EngineReg();
    var engineBootstrapper = new EngineBootstrapper(engineRegistry);

    // T0U Constitutional
    engineBootstrapper.Register(new PolicyValidationEngine());
    engineBootstrapper.Register(new PolicyEvaluationEngine());
    engineBootstrapper.Register(new GovernanceAuthorityEngine());
    engineBootstrapper.Register(new ConstitutionalSafeguardEngine());
    engineBootstrapper.Register(new ChainVerificationEngine());
    engineBootstrapper.Register(new IdentityVerificationEngine());

    // T1M Orchestration
    engineBootstrapper.Register(new WorkflowSchedulerEngine());
    engineBootstrapper.Register(new PartitionRouterEngine());
    engineBootstrapper.Register(new WorkflowGraphEngine());
    engineBootstrapper.Register(new RuntimeDispatcherEngine());
    engineBootstrapper.Register(new WorkflowStateProjectionEngine());

    // T2E Execution
    engineBootstrapper.Register(new RideExecutionEngine());
    engineBootstrapper.Register(new PropertyExecutionEngine());
    engineBootstrapper.Register(new EconomicExecutionEngine());
    engineBootstrapper.Register(new VaultCreationEngine());
    engineBootstrapper.Register(new CapitalContributionEngine());
    engineBootstrapper.Register(new AssetRegistrationEngine());
    engineBootstrapper.Register(new RevenueRecordingEngine());
    engineBootstrapper.Register(new ProfitDistributionEngine());

    // T3I Intelligence
    engineBootstrapper.Register(new DriverMatchingEngine());
    engineBootstrapper.Register(new TenantMatchingEngine());
    engineBootstrapper.Register(new WorkforceAssignmentEngine());
    engineBootstrapper.Register(new ObservabilityEngine());
    engineBootstrapper.Register(new AnalyticsEngine());
    engineBootstrapper.Register(new ForecastEngine());

    // T4A Access
    engineBootstrapper.Register(new AuthenticationEngine());
    engineBootstrapper.Register(new AuthorizationEngine());
    engineBootstrapper.Register(new APIEngine());
    engineBootstrapper.Register(new DeveloperToolsEngine());
    engineBootstrapper.Register(new OperatorControlPlaneEngine());
    engineBootstrapper.Register(new IntegrationEngine());

    builder.Services.AddSingleton<IEngineReg>(engineRegistry);
    builder.Services.AddSingleton(engineBootstrapper);

    var engineResolver = new EngineResolver(engineRegistry);
    builder.Services.AddSingleton(engineResolver);

    var engineInvocationManager = new EngineInvocationManager();
    builder.Services.AddSingleton(engineInvocationManager);

    var workflowStepEngineExecutor = new WorkflowStepEngineExecutor(engineResolver, engineInvocationManager);
    builder.Services.AddSingleton(workflowStepEngineExecutor);

    // Legacy engine dispatcher (for backward compatibility)
    var legacyEngineRegistry = new Whycespace.Runtime.Registry.EngineRegistry();
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

    var partitionManager = new PartitionManager();
    builder.Services.AddSingleton(partitionManager);

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

    // Projections
    var driverLocationProjection = new DriverLocationProjection();
    var propertyListingProjection = new PropertyListingProjection();
    var vaultBalanceProjection = new VaultBalanceProjection();
    var revenueProjection = new RevenueProjection();
    builder.Services.AddSingleton(driverLocationProjection);
    builder.Services.AddSingleton(propertyListingProjection);
    builder.Services.AddSingleton(vaultBalanceProjection);
    builder.Services.AddSingleton(revenueProjection);
    builder.Services.AddSingleton<IReadOnlyList<IProjection>>(new IProjection[]
    {
        driverLocationProjection,
        propertyListingProjection,
        vaultBalanceProjection,
        revenueProjection
    });

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

    // Runtime Dispatcher
    var workflowResolver = new WorkflowResolver();
    builder.Services.AddSingleton<IWorkflowResolver>(workflowResolver);

    var runtimeDispatcher = new RtDispatcher(commandValidator, commandIdempotency, workflowResolver, workflowRuntime);
    builder.Services.AddSingleton(runtimeDispatcher);
    builder.Services.AddSingleton<IRtDispatcher>(runtimeDispatcher);

    var executionPipeline = new ExecutionPipeline(commandValidator, commandIdempotency, workflowResolver, workflowRuntime);
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
