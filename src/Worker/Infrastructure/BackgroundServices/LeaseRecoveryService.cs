using Shared.Domain;
using Shared.Infrastructure.Interfaces;

namespace Worker.Infrastructure.BackgroundServices;

public class LeaseRecoveryService : BackgroundService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ILogger<LeaseRecoveryService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public LeaseRecoveryService(ITaskRepository taskRepository, ILogger<LeaseRecoveryService> logger)
    {
        _taskRepository = taskRepository;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var stuckTasks = await _taskRepository.GetStuckTasksAsync(stoppingToken);
                foreach (var task in stuckTasks)
                {
                    task.Status = ImageTaskStatus.Pending;
                    task.LeaseOwner = string.Empty;
                    task.LeaseUntil =DateTime.UtcNow.AddMinutes(5);
                    task.LastUpdated = DateTime.UtcNow;
                    await _taskRepository.UpdateAsync(task, stoppingToken);
                }
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Lease recovery failed");
            }
            
            await Task.Delay(_interval, stoppingToken);
        }
    }
}