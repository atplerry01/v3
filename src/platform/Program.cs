using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Whycespace.Platform.RuntimeClient;
using Whycespace.Runtime.Dispatcher;
using Whycespace.Runtime.Events;
using Whycespace.Runtime.Observability;
using Whycespace.Runtime.Projections;
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

// Projections (read-only access)
builder.Services.AddSingleton(new DriverLocationProjection());
builder.Services.AddSingleton(new PropertyListingProjection());
builder.Services.AddSingleton(new VaultBalanceProjection());
builder.Services.AddSingleton(new RevenueProjection());

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

// Upstream
builder.Services.AddSingleton(new PolicyGovernor());

// Kafka Publisher
builder.Services.AddSingleton(new KafkaEventPublisher(eventBus, kafkaBrokers));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
