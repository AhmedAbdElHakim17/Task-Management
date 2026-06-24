using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Requests;

public class CreateTaskRequest
{
    public string Title { get; set; }
    public string Description { get; set; }
    public TaskItemPriority Priority { get; set; }
}
