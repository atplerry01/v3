using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Whycespace.Platform.RuntimeClient;
using Whycespace.Runtime.Dispatcher;
using Whycespace.Runtime.Events;
using Whycespace.Runtime.Observability;
using Whycespace.Projections.Core.Economics;
using Whycespace.Projections.Clusters.Mobility;
using Whycespace.Projections.Clusters.Property;
using Whycespace.Projections.Queries;
using Whycespace.ProjectionRuntime.Storage;
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
using Whycespace.Domain.Clusters;
using Whycespace.Domain.Core.Cluster;
using Whycespace.Domain.Core.Providers;
using Whycespace.Domain.Core.Registry;
using Whycespace.SimulationRuntime.Loader;
using Whycespace.SimulationRuntime.Runtime;
using Whycespace.SimulationRuntime.Services;
using Whycespace.ClusterTemplatePlatform;
using Whycespace.Domain.Core.Economic;
using Whycespace.RuntimeValidation.Runners;
using Whycespace.Runtime.PlatformDispatch;

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
var identityRegistry = new Whycespace.System.WhyceID.Registry.IdentityRegistry();
var identityAttributeStore = new Whycespace.System.WhyceID.Stores.IdentityAttributeStore();
var identityRoleStore = new Whycespace.System.WhyceID.Stores.IdentityRoleStore();
var identityPermissionStore = new Whycespace.System.WhyceID.Stores.IdentityPermissionStore();
var identityAccessScopeStore = new Whycespace.System.WhyceID.Stores.IdentityAccessScopeStore();
var identityTrustStore = new Whycespace.System.WhyceID.Stores.IdentityTrustStore();
var identityDeviceStore = new Whycespace.System.WhyceID.Stores.IdentityDeviceStore();
var identitySessionStore = new Whycespace.System.WhyceID.Stores.IdentitySessionStore();
var identityConsentStore = new Whycespace.System.WhyceID.Stores.IdentityConsentStore();
var identityGraphStore = new Whycespace.System.WhyceID.Stores.IdentityGraphStore();
var identityServiceStore = new Whycespace.System.WhyceID.Stores.IdentityServiceStore();
var identityFederationStore = new Whycespace.System.WhyceID.Stores.IdentityFederationStore();
var identityRecoveryStore = new Whycespace.System.WhyceID.Stores.IdentityRecoveryStore();
var identityRevocationStore = new Whycespace.System.WhyceID.Stores.IdentityRevocationStore();
var identityAuditStore = new Whycespace.System.WhyceID.Stores.IdentityAuditStore();
builder.Services.AddSingleton(identityRegistry);
builder.Services.AddSingleton(identityAttributeStore);
builder.Services.AddSingleton(identityRoleStore);
builder.Services.AddSingleton(identityPermissionStore);
builder.Services.AddSingleton(identityAccessScopeStore);
builder.Services.AddSingleton(identityTrustStore);
builder.Services.AddSingleton(identityDeviceStore);
builder.Services.AddSingleton(identitySessionStore);
builder.Services.AddSingleton(identityConsentStore);
builder.Services.AddSingleton(identityGraphStore);
builder.Services.AddSingleton(identityServiceStore);
builder.Services.AddSingleton(identityFederationStore);
builder.Services.AddSingleton(identityRecoveryStore);
builder.Services.AddSingleton(identityRevocationStore);
builder.Services.AddSingleton(identityAuditStore);

// Upstream
builder.Services.AddSingleton(new PolicyGovernor());

// WhycePolicy (Phase 2.0.21+)
var policyRegistryStore = new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyRegistryStore();
var policyVersionStore = new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyVersionStore();
var policyDependencyStore = new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyDependencyStore();
var policyContextStore = new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyContextStore();
var policyDecisionCacheStore = new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyDecisionCacheStore();
var policyLifecycleStore = new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyLifecycleStore();
var policyRolloutStore = new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyRolloutStore();
var governanceAuthorityStore = new Whycespace.System.Upstream.WhycePolicy.Stores.GovernanceAuthorityStore();
var constitutionalPolicyStore = new Whycespace.System.Upstream.WhycePolicy.Stores.ConstitutionalPolicyStore();
var policyDomainBindingStore = new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyDomainBindingStore();
var policyMonitoringStore = new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyMonitoringStore();
var policyEvidenceStore = new Whycespace.System.Upstream.WhycePolicy.Stores.PolicyEvidenceStore();
builder.Services.AddSingleton(policyRegistryStore);
builder.Services.AddSingleton(policyVersionStore);
builder.Services.AddSingleton(policyDependencyStore);
builder.Services.AddSingleton(policyContextStore);
builder.Services.AddSingleton(policyDecisionCacheStore);
builder.Services.AddSingleton(policyLifecycleStore);
builder.Services.AddSingleton(policyRolloutStore);
builder.Services.AddSingleton(governanceAuthorityStore);
builder.Services.AddSingleton(constitutionalPolicyStore);
builder.Services.AddSingleton(policyDomainBindingStore);
builder.Services.AddSingleton(policyMonitoringStore);
builder.Services.AddSingleton(policyEvidenceStore);

// WhyceChain (Phase 2.0.40+)
var chainLedgerStore = new Whycespace.System.Upstream.WhyceChain.Stores.ChainLedgerStore();
var chainBlockStore = new Whycespace.System.Upstream.WhyceChain.Stores.ChainBlockStore();
var chainEventStore = new Whycespace.System.Upstream.WhyceChain.Stores.ChainEventStore();
builder.Services.AddSingleton(chainLedgerStore);
builder.Services.AddSingleton(chainBlockStore);
builder.Services.AddSingleton(chainEventStore);
builder.Services.AddSingleton(new Whycespace.System.Upstream.WhyceChain.Stores.ChainIndexStore());

// Chain Evidence Gateway (Phase 2.0.49)
var chainLedgerEngine = new Whycespace.Engines.T0U.WhyceChain.ChainLedgerEngine(chainLedgerStore);
var evidenceHashEngine = new Whycespace.Engines.T0U.WhyceChain.EvidenceHashEngine();
var immutableEventLedgerEngine = new Whycespace.Engines.T0U.WhyceChain.ImmutableEventLedgerEngine(chainEventStore);
var evidenceAnchoringEngine = new Whycespace.Engines.T0U.WhyceChain.EvidenceAnchoringEngine(chainLedgerEngine, evidenceHashEngine, immutableEventLedgerEngine);
var merkleProofEngine = new Whycespace.Engines.T0U.WhyceChain.MerkleProofEngine();
var integrityVerificationEngine = new Whycespace.Engines.T0U.WhyceChain.IntegrityVerificationEngine(merkleProofEngine);
var chainEvidenceGateway = new Whycespace.Platform.WhyceChain.ChainEvidenceGateway(evidenceAnchoringEngine, evidenceHashEngine, integrityVerificationEngine, chainBlockStore);
builder.Services.AddSingleton(chainEvidenceGateway);
builder.Services.AddSingleton(integrityVerificationEngine);

// Governance (Phase 2.0.54+)
var guardianRegistryStore = new Whycespace.System.Upstream.Governance.Stores.GuardianRegistryStore();
var governanceRoleStore = new Whycespace.System.Upstream.Governance.Stores.GovernanceRoleStore();
var governanceDelegationStore = new Whycespace.System.Upstream.Governance.Stores.GovernanceDelegationStore();
builder.Services.AddSingleton(guardianRegistryStore);
builder.Services.AddSingleton(governanceRoleStore);
builder.Services.AddSingleton(governanceDelegationStore);

// Guardian Registry v2 (Phase 2.0.54 — GuardianRecord)
var guardianRecordStore = new Whycespace.System.Upstream.Governance.Stores.GuardianRecordStore();
var guardianRegistry = new Whycespace.System.Upstream.Governance.Registry.GuardianRegistry(guardianRecordStore);
builder.Services.AddSingleton<Whycespace.System.Upstream.Governance.Stores.IGuardianRegistryStore>(guardianRecordStore);
builder.Services.AddSingleton<Whycespace.System.Upstream.Governance.Registry.IGuardianRegistry>(guardianRegistry);

// Governance Proposal Registry (Phase 2.0.57)
var governanceProposalStore = new Whycespace.System.Upstream.Governance.Proposals.Stores.GovernanceProposalStore();
var governanceProposalRegistry = new Whycespace.System.Upstream.Governance.Proposals.Registry.GovernanceProposalRegistry(governanceProposalStore);
builder.Services.AddSingleton<Whycespace.System.Upstream.Governance.Proposals.Stores.IGovernanceProposalStore>(governanceProposalStore);
builder.Services.AddSingleton<Whycespace.System.Upstream.Governance.Proposals.Registry.IGovernanceProposalRegistry>(governanceProposalRegistry);

// Governance Proposal Type Engine (Phase 2.0.59)
var governanceProposalTypeStore = new Whycespace.System.Upstream.Governance.Stores.GovernanceProposalTypeStore();
var governanceProposalTypeEngine = new Whycespace.Engines.T0U.Governance.GovernanceProposalTypeEngine(governanceProposalTypeStore, guardianRegistryStore);
builder.Services.AddSingleton(governanceProposalTypeStore);
builder.Services.AddSingleton(governanceProposalTypeEngine);

// Governance Proposal Engine (Phase 2.0.58)
var governanceProposalEngineStore = new Whycespace.System.Upstream.Governance.Stores.GovernanceProposalStore();
var governanceProposalEngine = new Whycespace.Engines.T0U.Governance.GovernanceProposalEngine(governanceProposalEngineStore);
builder.Services.AddSingleton(governanceProposalEngineStore);
builder.Services.AddSingleton(governanceProposalEngine);

// Governance Domain Scope Engine (Phase 2.0.60)
var governanceDomainScopeStore = new Whycespace.System.Upstream.Governance.Stores.GovernanceDomainScopeStore();
var governanceDomainScopeEngine = new Whycespace.Engines.T0U.Governance.GovernanceDomainScopeEngine(governanceDomainScopeStore, guardianRegistryStore);
builder.Services.AddSingleton(governanceDomainScopeStore);
builder.Services.AddSingleton(governanceDomainScopeEngine);

// Voting Engine (Phase 2.0.61)
var governanceVoteStore = new Whycespace.System.Upstream.Governance.Stores.GovernanceVoteStore();
var votingEngine = new Whycespace.Engines.T0U.Governance.VotingEngine(governanceVoteStore, governanceProposalEngineStore, guardianRegistryStore);
builder.Services.AddSingleton(governanceVoteStore);
builder.Services.AddSingleton(votingEngine);

// Governance Emergency Engine (Phase 2.0.66)
var governanceEmergencyStore = new Whycespace.System.Upstream.Governance.Stores.GovernanceEmergencyStore();
var governanceEmergencyEngine = new Whycespace.Engines.T0U.Governance.GovernanceEmergencyEngine(governanceEmergencyStore, guardianRegistryStore);
builder.Services.AddSingleton(governanceEmergencyStore);
builder.Services.AddSingleton(governanceEmergencyEngine);

// Governance Evidence Recorder (Phase 2.0.67)
var engineChainEvidenceGateway = new Whycespace.Engines.T0U.WhyceChain.ChainEvidenceGateway(evidenceAnchoringEngine, evidenceHashEngine);
var governanceEvidenceRecorder = new Whycespace.Engines.T0U.Governance.GovernanceEvidenceRecorder(engineChainEvidenceGateway);
builder.Services.AddSingleton(governanceEvidenceRecorder);

// Capital Policy Enforcement Adapter (Phase 2.2.25)
var capitalRegistry = new Whycespace.System.Midstream.Capital.Registry.CapitalRegistry();
var capitalPolicyAdapter = new Whycespace.System.Midstream.Capital.Governance.CapitalPolicyEnforcementAdapter(capitalRegistry);
builder.Services.AddSingleton<Whycespace.System.Midstream.Capital.Registry.ICapitalRegistry>(capitalRegistry);
builder.Services.AddSingleton(capitalRegistry);
builder.Services.AddSingleton(capitalPolicyAdapter);

// Capital Evidence Recorder (Phase 2.2.28)
var capitalEvidenceRecorder = new Whycespace.System.Midstream.Capital.Evidence.CapitalEvidenceRecorder();
builder.Services.AddSingleton<Whycespace.System.Midstream.Capital.Evidence.ICapitalEvidenceRecorder>(capitalEvidenceRecorder);
builder.Services.AddSingleton(capitalEvidenceRecorder);

// Capital Ledger Store (Phase 2.2.29)
var capitalLedgerStore = new Whycespace.System.Midstream.Capital.Stores.CapitalLedgerStore();
builder.Services.AddSingleton<Whycespace.System.Midstream.Capital.Stores.ICapitalLedgerStore>(capitalLedgerStore);
builder.Services.AddSingleton(capitalLedgerStore);

var capitalLifecycleEngine = new Whycespace.Engines.T3I.Capital.CapitalLifecycleEngine();
builder.Services.AddSingleton(capitalLifecycleEngine);

// WSS Runtime (Phase 2.1.x) — engines bootstrapped in runtime layer
var wssBootstrapper = new WssRuntimeBootstrapper(eventBus, kafkaBrokers);
builder.Services.AddSingleton(wssBootstrapper);

// Platform Runtime Dispatcher — single entry point for all engine operations
var platformDispatcher = PlatformDispatcherFactory.Create(
    identityRegistry,
    identityAttributeStore,
    identityRoleStore,
    identityPermissionStore,
    identityAccessScopeStore,
    identityTrustStore,
    identityDeviceStore,
    identitySessionStore,
    identityConsentStore,
    identityGraphStore,
    identityServiceStore,
    identityFederationStore,
    identityRecoveryStore,
    identityRevocationStore,
    identityAuditStore,
    policyRegistryStore,
    policyVersionStore,
    policyDependencyStore,
    policyContextStore,
    policyDecisionCacheStore,
    policyLifecycleStore,
    policyRolloutStore,
    governanceAuthorityStore,
    constitutionalPolicyStore,
    policyDomainBindingStore,
    policyMonitoringStore,
    policyEvidenceStore,
    guardianRegistryStore,
    governanceRoleStore,
    governanceDelegationStore,
    wssBootstrapper);
builder.Services.AddSingleton<IPlatformDispatcher>(platformDispatcher);

// Kafka Publisher
var kafkaPublisher = new KafkaEventPublisher(eventBus, kafkaBrokers);
builder.Services.AddSingleton(kafkaPublisher);

// Runtime Validation (Phase 1.17)
var validationRunner = new ValidationRunner();
builder.Services.AddSingleton(validationRunner);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
