using LogicFit.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Provisioning;

public sealed class ProvisioningWorker(
    IProvisioningQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ProvisioningWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverAcceptedOperationsAsync(stoppingToken);

        await foreach (var operationId in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IProvisioningWorkflow>().ProcessAsync(operationId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception)
            {
                // The workflow persists safe failure metadata. The worker log
                // intentionally contains identifiers only, never provider data.
                logger.LogError("Provisioning worker failed while processing operation {OperationId}.", operationId);
            }
        }
    }

    private async Task RecoverAcceptedOperationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var workflow = scope.ServiceProvider.GetRequiredService<IProvisioningWorkflow>();
            foreach (var operationId in await workflow.GetRecoverableRunIdsAsync(cancellationToken))
            {
                await queue.EnqueueAsync(operationId, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            logger.LogError("Provisioning worker could not recover accepted operations at startup.");
        }
    }
}
