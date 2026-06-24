namespace TaskManagement.Application.Interfaces;

public interface ITaskQueue
{
    void Enqueue(Guid taskId);
}
