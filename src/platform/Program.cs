using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Whycespace.Engines.T0U_Constitutional;
using Whycespace.Engines.T1M_Orchestration;
using Whycespace.Engines.T2E_Execution;
using Whycespace.Engines.T3I_Intelligence;
using Whycespace.Engines.T4A_Access;
using Whycespace.Runtime.Dispatcher;
using Whycespace.Runtime.Events;
using Whycespace.Runtime.Observability;
using Whycespace.Runtime.Persistence;
using Whycespace.Runtime.Projections;
using Whycespace.Runtime.Registry;
using Whycespace.Runtime.Reliability;
using Whycespace.Runtime.Workflow;
using Whycespace.System.Downstream.Clusters;
using Whycespace.System.Midstream.WSS.Dispatcher;
using Whycespace.System.Midstream.WSS.Kafka;
using Whycespace.System.Midstream.WSS.Mapping;
using Whycespace.System.Midstream.WSS.Orchestration;
using Whycespace.System.Midstream.WSS.Routing;
using Whycespace.System.Midstream.WSS.Workflows;
using Whycespace.System.Upstream.WhycePolicy;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Whycespace.Platform")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
builder.Host.UseSerilog();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Whycespace.Platform"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Whycespace.Runtime")
        .AddSource("Whycespace.Engines"));

// Health checks
var postgresConn = builder.Configuration.GetConnectionString("Postgres") ?? "Host=localhost;Database=whycespace;Username=whyce;Password=whyce";
var redisConn = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
var kafkaBrokers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
builder.Services.AddHealthChecks()
    .AddNpgSql(postgresConn, name: "postgres")
    .AddRedis(redisConn, name: "redis")
    .AddKafka(cfg => cfg.BootstrapServers = kafkaBrokers, name: "kafka");

// Engine Registry — all 31 engines across 5 tiers
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

// Runtime
var dispatcher = new RuntimeDispatcher(engineRegistry);
builder.Services.AddSingleton(dispatcher);

var workflowStateStore = new WorkflowStateStore();
builder.Services.AddSingleton(workflowStateStore);

var orchestrator = new WorkflowOrchestrator(dispatcher, workflowStateStore);
builder.Services.AddSingleton(orchestrator);

var eventBus = new EventBus();
builder.Services.AddSingleton(eventBus);

// Projections
var driverLocationProjection = new DriverLocationProjection();
var propertyListingProjection = new PropertyListingProjection();
var vaultBalanceProjection = new VaultBalanceProjection();
var revenueProjection = new RevenueProjection();
builder.Services.AddSingleton(driverLocationProjection);
builder.Services.AddSingleton(propertyListingProjection);
builder.Services.AddSingleton(vaultBalanceProjection);
builder.Services.AddSingleton(revenueProjection);

// Reliability
builder.Services.AddSingleton(new IdempotencyRegistry());
builder.Services.AddSingleton(new RetryPolicyEngine());
builder.Services.AddSingleton(new TimeoutManager());
builder.Services.AddSingleton(new DeadLetterQueue());

// Observability
builder.Services.AddSingleton(new RuntimeObserver());

// Workflow Mapper
var workflowMapper = new WorkflowMapper();
workflowMapper.Register(new RideRequestWorkflow());
workflowMapper.Register(new PropertyListingWorkflow());
workflowMapper.Register(new EconomicLifecycleWorkflow());
builder.Services.AddSingleton(workflowMapper);

// WSS
var wssOrchestrator = new WSSOrchestrator(workflowMapper, orchestrator);
builder.Services.AddSingleton(wssOrchestrator);

var workflowRouter = new WorkflowRouter();
workflowRouter.MapCommand<Whycespace.Domain.Application.Commands.RequestRideCommand>("RideRequest");
workflowRouter.MapCommand<Whycespace.Domain.Application.Commands.ListPropertyCommand>("PropertyListing");
workflowRouter.MapCommand<Whycespace.Domain.Application.Commands.AllocateCapitalCommand>("EconomicLifecycle");
builder.Services.AddSingleton(workflowRouter);

var commandDispatcher = new CommandDispatcher(workflowRouter, wssOrchestrator);
builder.Services.AddSingleton(commandDispatcher);

// Clusters
var clusterRegistry = new ClusterRegistry();
clusterRegistry.RegisterCluster(WhyceMobility.CreateCluster());
clusterRegistry.RegisterSubCluster(WhyceMobility.TaxiSubCluster());
clusterRegistry.RegisterCluster(WhyceProperty.CreateCluster());
clusterRegistry.RegisterSubCluster(WhyceProperty.PropertyLettingSubCluster());
builder.Services.AddSingleton(clusterRegistry);

// Upstream
builder.Services.AddSingleton(new PolicyGovernor());

// Persistence
builder.Services.AddSingleton(new PostgresEventStore(postgresConn));
builder.Services.AddSingleton(new ProjectionStore(postgresConn));
builder.Services.AddSingleton(new WorkflowStateRepository(postgresConn));

// Kafka Publisher
builder.Services.AddSingleton(new KafkaEventPublisher(eventBus, kafkaBrokers));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
