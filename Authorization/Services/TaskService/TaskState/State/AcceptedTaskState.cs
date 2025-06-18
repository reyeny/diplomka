using System;
using Authorization.enums;
using Authorization.Models;

namespace Authorization.Services.TaskService.TaskState.State
{
    public class AcceptedTaskState : TaskStateBase
    {
        public override TaskItemStatus Status => TaskItemStatus.Accepted;

        public override void Complete(TaskItem task, string userId)
        {
            if (task.AssignedToId != userId)
                throw new InvalidOperationException("Только назначенный может завершить задачу.");

            task.Status      = TaskItemStatus.Done;
            task.UpdatedAt = DateTime.UtcNow;
        }

        public override void Unassign(TaskItem task)
        {
            // Сбрасываем задачу обратно в New
            task.Status       = TaskItemStatus.New;
            task.AssignedToId = null;
        }
    }
}