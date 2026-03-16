namespace Whycespace.Platform.Dispatch;

using Whycespace.Engines.T1M.WSS.Stores;
using Whycespace.Engines.T1M.WSS.Registry;
using Whycespace.Engines.T1M.WSS.Graph;
using Whycespace.Engines.T1M.WSS.Instance;
using Whycespace.Engines.T1M.WSS.Runtime;
using Whycespace.Runtime.EventFabric.WSS;
using Whycespace.Runtime.Events;

/// <summary>
/// Bootstraps all WSS engine stores and instances that were previously
/// constructed in Platform/Program.cs. This keeps engine construction
/// inside the runtime layer.
/// </summary>
public sealed class WssRuntimeBootstrapper
{
    public WorkflowDefinitionStore WorkflowDefinitionStore { get; }
    public WorkflowTemplateStore WorkflowTemplateStore { get; }
    public WorkflowRegistryStore WorkflowRegistryStore { get; }
    public WorkflowVersionStore WorkflowVersionStore { get; }
    public WorkflowRegistry WorkflowRegistry { get; }
    public WorkflowEngineMappingStore EngineMappingStore { get; }
    public WorkflowInstanceRegistryStore InstanceRegistryStore { get; }
    public WssWorkflowStateStore WorkflowStateStore { get; }
    public WorkflowEventRouter WorkflowEventRouter { get; }
    public WorkflowRetryStore RetryStore { get; }
    public WorkflowRetryPolicyEngine RetryPolicyEngine { get; }
    public WorkflowTimeoutStore TimeoutStore { get; }
    public WorkflowTimeoutEngine TimeoutEngine { get; }
    public WorkflowLifecycleEngine LifecycleEngine { get; }

    public WssRuntimeBootstrapper(EventBus eventBus, string kafkaBrokers)
    {
        WorkflowDefinitionStore = new WorkflowDefinitionStore();
        WorkflowTemplateStore = new WorkflowTemplateStore();
        WorkflowRegistryStore = new WorkflowRegistryStore();
        WorkflowVersionStore = new WorkflowVersionStore();
        WorkflowRegistry = new WorkflowRegistry();
        EngineMappingStore = new WorkflowEngineMappingStore();
        InstanceRegistryStore = new WorkflowInstanceRegistryStore();
        WorkflowStateStore = new WssWorkflowStateStore();

        var kafkaPublisher = new KafkaEventPublisher(eventBus, kafkaBrokers);
        WorkflowEventRouter = new WorkflowEventRouter(kafkaPublisher);

        RetryStore = new WorkflowRetryStore();
        RetryPolicyEngine = new WorkflowRetryPolicyEngine(RetryStore);

        TimeoutStore = new WorkflowTimeoutStore();
        TimeoutEngine = new WorkflowTimeoutEngine(TimeoutStore);

        var instanceRegistry = new WorkflowInstanceRegistry(InstanceRegistryStore);
        var graphEngine = new WorkflowGraphEngine();

        LifecycleEngine = new WorkflowLifecycleEngine(
            WorkflowRegistry,
            instanceRegistry,
            WorkflowStateStore,
            WorkflowEventRouter,
            RetryPolicyEngine,
            RetryStore,
            TimeoutEngine,
            graphEngine);
    }
}
