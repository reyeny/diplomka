using Authorization.Context;
using Authorization.Dto.Company;
using Authorization.enums;
using Authorization.Models;
using Authorization.Services.InvitationService;
using Authorization.Services.TaskStateService.TaskState;
using Microsoft.EntityFrameworkCore;

namespace Authorization.Services.TaskService
{
    public class TaskService(UnchainMeDbContext db) : ITaskService
    {
        public async Task<TaskDto> CreateTaskAsync(string userId, Guid companyId, CreateTaskDto dto)
        {
            var task = new TaskItem
            {
                Id            = Guid.NewGuid(),
                CompanyId     = companyId,
                Title         = dto.Title,
                Description   = dto.Description,
                CreatedById   = userId,
                Status        = TaskItemStatus.New,
                CreatedAt     = DateTime.UtcNow,
                UpdatedAt     = DateTime.UtcNow,
                AssignedToId  = dto.AssignedToUserId?.ToString()
            };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            return Map(task);
        }

        public async Task<IEnumerable<TaskDto>> ListTasksAsync(string userId, Guid companyId)
        {
            return await db.Tasks
                .AsNoTracking()
                .Where(t => t.CompanyId == companyId)
                .Select(t => Map(t))
                .ToListAsync();
        }

        public async Task ClaimTaskAsync(string userId, Guid taskId)
        {
            var task = await db.Tasks.FindAsync(taskId)
                       ?? throw new ArgumentException("Задача не найдена");

            // Делегируем логику изменения состояния
            var state = TaskStateFactory.Get(task.Status);
            state.Claim(task, userId);

            task.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        public async Task ChangeStatusAsync(string userId, Guid taskId, ChangeTaskStatusDto dto)
        {
            var task = await db.Tasks.FindAsync(taskId)
                       ?? throw new ArgumentException("Задача не найдена");

            // Новый статус из dto
            if (!Enum.TryParse<TaskItemStatus>(dto.Status.ToString(), out var desiredStatus))
                throw new ArgumentException($"Неверный статус: {dto.Status}");

            // Если это попытка завершить — используем Complete
            if (desiredStatus == TaskItemStatus.Done)
            {
                var state = TaskStateFactory.Get(task.Status);
                state.Complete(task, userId);
            }
            else
            {
                throw new InvalidOperationException($"Нельзя установить статус {desiredStatus} напрямую");
            }

            task.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        private static TaskDto Map(TaskItem t)
            => new TaskDto
            {
                Id           = t.Id,
                CompanyId    = t.CompanyId,
                Title        = t.Title,
                Description  = t.Description,
                CreatedById  = t.CreatedById,
                AssignedToId = t.AssignedToId,
                Status       = t.Status,
                CreatedAt    = t.CreatedAt,
                UpdatedAt    = t.UpdatedAt
            };
    }
}
