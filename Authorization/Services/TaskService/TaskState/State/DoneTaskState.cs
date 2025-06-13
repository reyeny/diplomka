using Authorization.enums;

namespace Authorization.Services.TaskStateService.TaskState.State;

public class DoneTaskState : TaskStateBase
{
    public override TaskItemStatus Status => TaskItemStatus.Done;
    // нет переходов дальше
}
