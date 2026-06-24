using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.BackgroundServices;

public class TaskProcessingService(Channel<Guid> channel,IServiceScopeFactory scopeFactory,ILogger<TaskProcessingService> logger) : BackgroundService
{
    private readonly ChannelReader<Guid> _reader = channel.Reader;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[BackgroundService] TaskProcessingService started — waiting for queued tasks");

        await foreach (var taskId in _reader.ReadAllAsync(stoppingToken))
        {
            logger.LogInformation("[BackgroundService] Dequeued task {TaskId} — beginning processing", taskId);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, stoppingToken);
                if (task is null)
                {
                    logger.LogWarning("[BackgroundService] Task {TaskId} not found in database — skipping", taskId);
                    continue;
                }

                // Simulate async processing work
                await Task.Delay(2000, stoppingToken);

                task.Status = TaskItemStatus.InProgress;
                await db.SaveChangesAsync(stoppingToken);

                logger.LogInformation(
                    "[BackgroundService] Task {TaskId} processed successfully — status set to {Status}",
                    taskId, task.Status);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("[BackgroundService] Processing cancelled for task {TaskId}", taskId);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[BackgroundService] Failed to process task {TaskId}", taskId);
            }
        }

        logger.LogInformation("[BackgroundService] TaskProcessingService stopped");
    }
}
