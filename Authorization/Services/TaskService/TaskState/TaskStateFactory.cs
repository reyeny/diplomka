using Authorization.enums;
using Authorization.Services.TaskService.TaskState.State;
namespace Authorization.Services.TaskService.TaskState;

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
