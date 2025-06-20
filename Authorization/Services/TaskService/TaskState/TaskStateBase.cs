using Authorization.enums;
using Authorization.Models;

namespace Authorization.Services.TaskService.TaskState;

public abstract class TaskStateBase : ITaskState
{
    public abstract TaskItemStatus Status { get; }

    public virtual void Claim(TaskItem task, string userId)
        => throw new InvalidOperationException(
            $"Невозможно взять задачу в состоянии {task.Status}");

    public virtual void Complete(TaskItem task, string userId)
        => throw new InvalidOperationException(
            $"Невозможно завершить задачу в состоянии {task.Status}");

    public virtual void Unassign(TaskItem task)
        => throw new InvalidOperationException(
            $"Невозможно сбросить задачу в состоянии {task.Status}");
}