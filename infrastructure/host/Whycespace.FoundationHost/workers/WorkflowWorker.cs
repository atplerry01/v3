namespace Whycespace.FoundationHost.Workers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Whycespace.Runtime.Dispatcher;
using Whycespace.Runtime.Workflow;
using Whycespace.Shared.Contracts;

public sealed class WorkflowWorker : BackgroundService
{
    private readonly RuntimeDispatcher _dispatcher;
    private readonly WorkflowStateStore _stateStore;
    private readonly ILogger<WorkflowWorker> _logger;

    public WorkflowWorker(
        RuntimeDispatcher dispatcher,
        WorkflowStateStore stateStore,
        ILogger<WorkflowWorker> logger)
    {
        _dispatcher = dispatcher;
        _stateStore = stateStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WorkflowWorker started — consuming workflow commands");

        while (!stoppingToken.IsCancellationRequested)
        {
            var pending = _stateStore.GetAll()
                .Where(s => s.Status == Shared.Workflow.WorkflowStatus.Pending)
                .ToList();

            foreach (var workflow in pending)
            {
                _logger.LogDebug("Processing pending workflow {WorkflowId}", workflow.WorkflowId);

                var envelope = new EngineInvocationEnvelope(
                    Guid.NewGuid(),
                    "WorkflowScheduler",
                    workflow.WorkflowId,
                    workflow.CurrentStepId,
                    workflow.WorkflowId,
                    workflow.Context);

                await _dispatcher.DispatchAsync(envelope);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        _logger.LogInformation("WorkflowWorker stopped");
    }
}
