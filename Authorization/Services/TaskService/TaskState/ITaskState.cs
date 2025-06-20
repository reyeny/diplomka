using Authorization.enums;
using Authorization.Models;

namespace Authorization.Services.TaskService.TaskState;

public interface ITaskState
{
    TaskItemStatus Status { get; }
    void Claim(TaskItem task, string userId);
    void Complete(TaskItem task, string userId);
    void Unassign(TaskItem task);
}