using Authorization.enums;
using Authorization.Models;

namespace Authorization.Services.TaskService.TaskState.State;

public class NewTaskState : TaskStateBase
{
    public override TaskItemStatus Status => TaskItemStatus.New;
    public override void Claim(TaskItem task, string userId)
    {
        task.AssignedToId = userId;
        task.Status       = TaskItemStatus.Accepted;
    }
}