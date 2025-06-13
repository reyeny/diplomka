using Authorization.enums;
using Authorization.Services.TaskStateService.TaskState.State;

namespace Authorization.Services.TaskStateService.TaskState;

public static class TaskStateFactory
{
    private static readonly Dictionary<TaskItemStatus, ITaskState> States = new()
    {
        { TaskItemStatus.New,      new NewTaskState() },
        { TaskItemStatus.Accepted, new AcceptedTaskState() },
        { TaskItemStatus.Done,     new DoneTaskState() }
    };

    public static ITaskState Get(TaskItemStatus status)
        => States.TryGetValue(status, out var s)
            ? s
            : throw new ArgumentOutOfRangeException(nameof(status));
}
