using Authorization.Dto.Company;

namespace Authorization.Services.InvitationService;

public interface ITaskService
{
    Task<TaskDto> CreateTaskAsync(string userId, Guid companyId, CreateTaskDto dto);
    Task<IEnumerable<TaskDto>> ListTasksAsync(string userId, Guid companyId);
    Task ClaimTaskAsync(string userId, Guid taskId);
    Task ChangeStatusAsync(string userId, Guid taskId, ChangeTaskStatusDto dto);
}