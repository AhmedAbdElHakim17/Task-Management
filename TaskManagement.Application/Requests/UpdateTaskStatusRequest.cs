using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Requests;

public class UpdateTaskStatusRequest
{
    public TaskItemStatus NewStatus { get; set; }
}
