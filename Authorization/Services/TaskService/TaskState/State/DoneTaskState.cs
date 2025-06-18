using Authorization.enums;

namespace Authorization.Services.TaskService.TaskState.State;

public class DoneTaskState : TaskStateBase
{
    public override TaskItemStatus Status => TaskItemStatus.Done;
}
