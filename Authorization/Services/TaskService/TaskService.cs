using Authorization.Context;
using Authorization.Dto.Company;
using Authorization.enums;
using Authorization.Exceptions;
using Authorization.Helpers;
using Authorization.Models;
using Authorization.Services.InvitationService;
using Authorization.Services.TaskService.TaskState;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Authorization.Services.TaskService
{
    public class TaskService(UnchainMeDbContext db, IHubContext<NotificationHub> hub) : ITaskService
    {
        public async Task<TaskDto> CreateTaskAsync(string userId, Guid companyId, CreateTaskDto dto)
        {
            var task = new TaskItem
            {
                Id           = Guid.NewGuid(),
                CompanyId    = companyId,
                Title        = dto.Title,
                Description  = dto.Description,
                CreatedById  = userId,
                Status       = TaskItemStatus.New,
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow,
                AssignedToId = dto.AssignedToUserId?.ToString()
            };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            return Map(task);
        }

        public async Task<IEnumerable<TaskDto>> ListTasksAsync(string userId, Guid companyId)
        {
            var cu = await db.CompanyUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.UserId == userId);
            if (cu == null)
                throw new AppException("У вас нет доступа к этой компании");

            var list = await db.Tasks
                .AsNoTracking()
                .Where(t => t.CompanyId == companyId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return list.Select(Map);
        }

        public async Task ClaimTaskAsync(string userId, Guid taskId)
        {
            var task = await db.Tasks.FindAsync(taskId)
                       ?? throw new AppException("Задача не найдена");

            var cu = await db.CompanyUsers
                .FirstOrDefaultAsync(x => x.CompanyId == task.CompanyId && x.UserId == userId);
            if (cu == null || cu.Role != CompanyRole.Employee)
                throw new AppException("У вас нет прав брать задачу");

            var state = TaskStateFactory.Get(task.Status);
            state.Claim(task, userId);

            task.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        public async Task ChangeStatusAsync(string userId, Guid taskId, ChangeTaskStatusDto dto)
        {
            var task = await db.Tasks.FindAsync(taskId)
                       ?? throw new AppException("Задача не найдена");

            if (dto.Status == TaskItemStatus.Done)
            {
                var cu = await db.CompanyUsers
                    .FirstOrDefaultAsync(x => x.CompanyId == task.CompanyId && x.UserId == userId);
                if (cu == null)
                    throw new AppException("У вас нет доступа к этой компании");

                var state = TaskStateFactory.Get(task.Status);
                state.Complete(task, userId);
            }
            else
            {
                throw new AppException($"Недопустимый целевой статус: {dto.Status}");
            }

            task.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        public async Task UnassignTaskAsync(string userId, Guid taskId)
        {
            var task = await db.Tasks.FindAsync(taskId)
                       ?? throw new AppException("Задача не найдена");

            // Только менеджер или админ могут сбрасывать задачу
            var cu = await db.CompanyUsers
                .FirstOrDefaultAsync(x => x.CompanyId == task.CompanyId && x.UserId == userId);
            if (cu == null || (cu.Role != CompanyRole.Manager && cu.Role != CompanyRole.Admin))
                throw new AppException("У вас нет прав сбрасывать задачу");

            // Вернём задачу в состояние New
            var state = TaskStateFactory.Get(task.Status);
            state.Unassign(task);

            task.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            
            if (!string.IsNullOrEmpty(task.AssignedToId))
            {
                await hub.Clients
                    .Group(task.AssignedToId)
                    .SendAsync("TaskCanceled", new {
                        TaskId    = task.Id,
                        Title     = task.Title,
                        Message   = "Задача была сброшена администратором."
                    });
            }
        }

        public async Task DeleteTaskAsync(string userId, Guid taskId)
        {
            var task = await db.Tasks.FindAsync(taskId)
                       ?? throw new AppException("Задача не найдена");
            
            var prevAssignee = task.AssignedToId;
            
            var cu = await db.CompanyUsers
                .FirstOrDefaultAsync(x => x.CompanyId == task.CompanyId && x.UserId == userId);
            if (cu == null || (cu.Role != CompanyRole.Manager && cu.Role != CompanyRole.Admin))
                throw new AppException("У вас нет прав удалять задачу");

            db.Tasks.Remove(task);
            await db.SaveChangesAsync();
            
            if (!string.IsNullOrEmpty(prevAssignee))
            {
                await hub.Clients
                    .Group(prevAssignee)
                    .SendAsync("TaskDeleted", new {
                        TaskId    = taskId,
                        Title     = task.Title,
                        Message   = "Задача была удалена администратором."
                    });
            }
        }

        private static TaskDto Map(TaskItem t)
            => new()
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
