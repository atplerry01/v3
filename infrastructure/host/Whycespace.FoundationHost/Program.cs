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
using Whycespace.WorkflowRuntime.Step;
using WfRuntime = Whycespace.WorkflowRuntime.Runtime.WorkflowRuntime;
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

    // Engine Registry — scan and register all 31 engines across 5 tiers
    var engineRegistry = new EngineRegistry();

    // T0U Constitutional
    engineRegistry.Register(new PolicyValidationEngine());
    engineRegistry.Register(new PolicyEvaluationEngine());
    engineRegistry.Register(new GovernanceAuthorityEngine());
    engineRegistry.Register(new ConstitutionalSafeguardEngine());
    engineRegistry.Register(new ChainVerificationEngine());
    engineRegistry.Register(new IdentityVerificationEngine());

    // T1M Orchestration
    engineRegistry.Register(new WorkflowSchedulerEngine());
    engineRegistry.Register(new PartitionRouterEngine());
    engineRegistry.Register(new WorkflowGraphEngine());
    engineRegistry.Register(new RuntimeDispatcherEngine());
    engineRegistry.Register(new WorkflowStateProjectionEngine());

    // T2E Execution
    engineRegistry.Register(new RideExecutionEngine());
    engineRegistry.Register(new PropertyExecutionEngine());
    engineRegistry.Register(new EconomicExecutionEngine());
    engineRegistry.Register(new VaultCreationEngine());
    engineRegistry.Register(new CapitalContributionEngine());
    engineRegistry.Register(new AssetRegistrationEngine());
    engineRegistry.Register(new RevenueRecordingEngine());
    engineRegistry.Register(new ProfitDistributionEngine());

    // T3I Intelligence
    engineRegistry.Register(new DriverMatchingEngine());
    engineRegistry.Register(new TenantMatchingEngine());
    engineRegistry.Register(new WorkforceAssignmentEngine());
    engineRegistry.Register(new ObservabilityEngine());
    engineRegistry.Register(new AnalyticsEngine());
    engineRegistry.Register(new ForecastEngine());

    // T4A Access
    engineRegistry.Register(new AuthenticationEngine());
    engineRegistry.Register(new AuthorizationEngine());
    engineRegistry.Register(new APIEngine());
    engineRegistry.Register(new DeveloperToolsEngine());
    engineRegistry.Register(new OperatorControlPlaneEngine());
    engineRegistry.Register(new IntegrationEngine());

    builder.Services.AddSingleton(engineRegistry);

    // Runtime services
    var engineDispatcher = new Whycespace.Runtime.Dispatcher.RuntimeDispatcher(engineRegistry);
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

    var stepExecutor = new WorkflowStepExecutor(engineDispatcher.DispatchAsync);
    builder.Services.AddSingleton(stepExecutor);

    var workflowExecutor = new WorkflowExecutor(stepExecutor);
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
        engines = engineRegistry.GetRegisteredEngines().Count,
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

    Log.Information("Foundation Host initialized with {EngineCount} engines", engineRegistry.GetRegisteredEngines().Count);

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
