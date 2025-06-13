using Authorization.enums;
using Authorization.Models;

namespace Authorization.Services.TaskStateService.TaskState.State;

public class AcceptedTaskState : TaskStateBase
{
    public override TaskItemStatus Status => TaskItemStatus.Accepted;
    public override void Complete(TaskItem task, string userId)
    {
        if (task.AssignedToId != userId)
            throw new InvalidOperationException("Только назначенный может завершить задачу.");

        task.Status = TaskItemStatus.Done;
    }
}
