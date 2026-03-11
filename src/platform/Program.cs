using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Whycespace.Platform.RuntimeClient;
using Whycespace.Runtime.Dispatcher;
using Whycespace.Runtime.Events;
using Whycespace.Runtime.Observability;
using Whycespace.Projections.Projections;
using Whycespace.Projections.Queries;
using Whycespace.Projections.Storage;
using Whycespace.Runtime.Registry;
using Whycespace.Runtime.Reliability;
using Whycespace.Runtime.Workflow;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Runtime;
using Whycespace.System.Downstream.Clusters;
using Whycespace.System.Midstream.WSS.Dispatcher;
using Whycespace.System.Midstream.WSS.Kafka;
using Whycespace.System.Midstream.WSS.Mapping;
using Whycespace.System.Midstream.WSS.Orchestration;
using Whycespace.System.Midstream.WSS.Routing;
using Whycespace.System.Midstream.WSS.Workflows;
using Whycespace.System.Upstream.WhycePolicy;
using Whycespace.ClusterDomain;
using Whycespace.SimulationRuntime.Loader;
using Whycespace.SimulationRuntime.Runtime;
using Whycespace.SimulationRuntime.Services;
using Whycespace.ClusterTemplatePlatform;
using Whycespace.EconomicDomain;
using Whycespace.RuntimeValidation.Runners;

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

// Runtime client adapters — in-process adapter for now
// FoundationHost owns the runtime; Platform connects via interfaces
var engineRegistry = new EngineRegistry();
builder.Services.AddSingleton(engineRegistry);

var eventBus = new EventBus();
builder.Services.AddSingleton(eventBus);
builder.Services.AddSingleton<IEventBus>(sp => new EventClient(sp.GetRequiredService<EventBus>()));

var dispatcher = new RuntimeDispatcher(engineRegistry);
builder.Services.AddSingleton<IEngineRuntimeDispatcher>(sp => new RuntimeClient(dispatcher));

var workflowStateStore = new WorkflowStateStore();
builder.Services.AddSingleton(workflowStateStore);

var orchestrator = new WorkflowOrchestrator(dispatcher, workflowStateStore);
builder.Services.AddSingleton<IWorkflowOrchestrator>(sp => new WorkflowClient(orchestrator));

// Projections (CQRS read side)
var projectionStore = new RedisProjectionStore();
builder.Services.AddSingleton<IProjectionStore>(projectionStore);
builder.Services.AddSingleton(new DriverLocationProjection(projectionStore));
builder.Services.AddSingleton(new RideStatusProjection(projectionStore));
builder.Services.AddSingleton(new PropertyListingProjection(projectionStore));
builder.Services.AddSingleton(new VaultBalanceProjection(projectionStore));
builder.Services.AddSingleton(new RevenueProjection(projectionStore));
builder.Services.AddSingleton(new ProjectionQueryService(projectionStore));

// Observability
builder.Services.AddSingleton(new RuntimeObserver());

// Reliability (read-only access for operator console)
builder.Services.AddSingleton(new DeadLetterQueue());

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

// Cluster Domain (Phase 1.13 + 1.14)
var clusterAdmin = new ClusterAdministrationService();
var clusterProviderRegistry = new ClusterProviderRegistry();
var spvRegistry = new SpvRegistry();
var providerAssignmentService = new ProviderAssignmentService();
var clusterBootstrapper = new ClusterBootstrapper(clusterAdmin, clusterProviderRegistry, spvRegistry, providerAssignmentService);
clusterBootstrapper.Bootstrap();
builder.Services.AddSingleton(clusterBootstrapper);

// Cluster Template Platform (Phase 1.14.5)
var clusterTemplateService = new ClusterTemplateService(clusterAdmin, clusterProviderRegistry, providerAssignmentService);
builder.Services.AddSingleton(clusterTemplateService);

// Economic Domain (Phase 1.15)
var spvEconomicRegistry = new SpvEconomicRegistry();
spvEconomicRegistry.RegisterSpv("WhyceMobility", "Taxi");
spvEconomicRegistry.RegisterSpv("WhyceProperty", "LettingAgent");
builder.Services.AddSingleton(spvEconomicRegistry);

// Simulation Runtime (Phase 1.13.5)
var simulationLoader = new SimulationScenarioLoader();
var simulationEngine = new SimulationRuntimeEngine();
var simulationService = new SimulationService(simulationLoader, simulationEngine);
builder.Services.AddSingleton(simulationService);

// WhyceID Identity (Phase 2.0)
builder.Services.AddSingleton(new Whycespace.System.WhyceID.Registry.IdentityRegistry());
builder.Services.AddSingleton(new Whycespace.System.WhyceID.Stores.IdentityAttributeStore());
builder.Services.AddSingleton(new Whycespace.System.WhyceID.Stores.IdentityRoleStore());
builder.Services.AddSingleton(new Whycespace.System.WhyceID.Stores.IdentityPermissionStore());
builder.Services.AddSingleton(new Whycespace.System.WhyceID.Stores.IdentityAccessScopeStore());
builder.Services.AddSingleton(new Whycespace.System.WhyceID.Stores.IdentityTrustStore());
builder.Services.AddSingleton(new Whycespace.System.WhyceID.Stores.IdentityDeviceStore());
builder.Services.AddSingleton(new Whycespace.System.WhyceID.Stores.IdentitySessionStore());
builder.Services.AddSingleton(new Whycespace.System.WhyceID.Stores.IdentityConsentStore());
builder.Services.AddSingleton(new Whycespace.System.WhyceID.Stores.IdentityGraphStore());
builder.Services.AddSingleton(new Whycespace.System.WhyceID.Stores.IdentityServiceStore());
builder.Services.AddSingleton(new Whycespace.System.WhyceID.Stores.IdentityFederationStore());
builder.Services.AddSingleton(new Whycespace.System.WhyceID.Stores.IdentityRecoveryStore());
builder.Services.AddSingleton(new Whycespace.System.WhyceID.Stores.IdentityRevocationStore());
builder.Services.AddSingleton(new Whycespace.System.WhyceID.Stores.IdentityAuditStore());

// Upstream
builder.Services.AddSingleton(new PolicyGovernor());

// WhycePolicy (Phase 2.0.21+)
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyRegistryStore());
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyVersionStore());
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyDependencyStore());
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyContextStore());
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyDecisionCacheStore());
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyLifecycleStore());
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyRolloutStore());
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhycePolicy.Stores.GovernanceAuthorityStore());
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhycePolicy.Stores.ConstitutionalPolicyStore());
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyDomainBindingStore());
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyMonitoringStore());
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyEvidenceStore());

// WhyceChain (Phase 2.0.40+)
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhyceChain.Stores.ChainLedgerStore());
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhyceChain.Stores.ChainBlockStore());
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhyceChain.Stores.ChainEventStore());
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhyceChain.Stores.ChainIndexStore());

// Runtime Validation (Phase 1.17)
var validationRunner = new ValidationRunner();
builder.Services.AddSingleton(validationRunner);

// Kafka Publisher
builder.Services.AddSingleton(new KafkaEventPublisher(eventBus, kafkaBrokers));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
