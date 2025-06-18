using Authorization.Dto;
using Authorization.Models;

namespace Authorization.Services.ApplicationService;

public interface IApplicationService
{
    Task<List<Application>> ListApplicationsWithUsersAsync(string userId, Guid companyId);
    Task<Application?> CreateApplicationAsync(string userId, Guid companyId, CreateApplicationDto dto);
    Task<Application> AssistantReviewAsync(string userId, Guid appId, AssistantReviewDto dto);
    Task<Application> DirectorReviewAsync(string userId, Guid appId, DirectorReviewDto dto);
    Task<ApplicationStatsDto> GetUserApplicationsStatsAsync(string userId);

}