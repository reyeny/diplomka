using Authorization.Context;
using Authorization.Dto;
using Authorization.enums;
using Authorization.Helpers;
using Authorization.Models;
using Authorization.Services.UserServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Authorization.Services.ApplicationService;

public class ApplicationService(
    UnchainMeDbContext db,
    IUserService userService,
    IHubContext<NotificationHub> hub) : IApplicationService 
{
    public async Task<List<Application>> ListApplicationsWithUsersAsync(string userId, Guid companyId)
    {
        var companyUser = await db.CompanyUsers
            .FirstOrDefaultAsync(x => x.UserId == userId && x.CompanyId == companyId);

        if (companyUser == null)
            throw new Exception("Нет доступа к компании");

        var query = db.Applications
            .Where(a => a.CompanyId == companyId);

        if (companyUser.Role == CompanyRole.Employee)
            query = query.Where(a => a.CreatedById == userId);

        query = query
            .Include(a => a.CreatedBy)
            .Include(a => a.Company)
            .ThenInclude(c => c.CompanyUsers)
            .ThenInclude(cu => cu.User);

        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
    }

    public async Task<Application?> CreateApplicationAsync(string userId, Guid companyId, CreateApplicationDto dto)
    {
        var companyUser = await db.CompanyUsers
            .FirstOrDefaultAsync(x => x.UserId == userId && x.CompanyId == companyId);
        if (companyUser is not { Role: CompanyRole.Employee })
            throw new Exception("Только сотрудники могут создавать заявки");

        var application = new Application
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CreatedById = userId,
            Type = dto.Type,
            CustomType = dto.CustomType,
            Comment = dto.Comment,
            Status = ApplicationStatus.New,
            CreatedAt = DateTime.UtcNow
        };

        db.Applications.Add(application);
        await db.SaveChangesAsync();

        var assistants = await db.CompanyUsers
            .Where(x => x.CompanyId == companyId && x.Role == CompanyRole.Assistant)
            .Select(x => x.UserId)
            .ToListAsync();

        foreach (var assistantId in assistants)
        {
            await hub.Clients.User(assistantId)
                .SendAsync("ReceiveNotification", new
                {
                    Title = "Новая заявка",
                    Message = "Появилась новая заявка на рассмотрение.",
                    ApplicationId = application.Id
                });
        }
        
        var result = await db.Applications
            .Where(a => a.Id == application.Id)
            .Include(a => a.CreatedBy)
            .Include(a => a.Company)
            .ThenInclude(c => c.CompanyUsers)
            .ThenInclude(cu => cu.User)
            .FirstOrDefaultAsync();

        return result;
    }

    public async Task<Application> AssistantReviewAsync(string userId, Guid appId, AssistantReviewDto dto)
    {
        var app = await db.Applications.Include(a => a.Company).FirstOrDefaultAsync(a => a.Id == appId);
        if (app == null)
            throw new Exception("Заявка не найдена");
        
        var companyUser = await db.CompanyUsers
            .FirstOrDefaultAsync(x => x.UserId == userId && x.CompanyId == app.CompanyId);
        if (companyUser == null || companyUser.Role != CompanyRole.Assistant)
            throw new Exception("Нет прав для рассмотрения");

        if (app.Status != ApplicationStatus.New)
            throw new Exception("Заявка уже была рассмотрена");

        app.AssistantReviewedAt = DateTime.UtcNow;
        app.AssistantComment = dto.Comment;
        app.Status = dto.Approve ? ApplicationStatus.AssistantApproved : ApplicationStatus.AssistantRejected;

        await db.SaveChangesAsync();

        if (dto.Approve)
        {
            var directors = await db.CompanyUsers
                .Where(x => x.CompanyId == app.CompanyId && x.Role == CompanyRole.Director)
                .Select(x => x.UserId)
                .ToListAsync();

            foreach (var directorId in directors)
            {
                await hub.Clients.User(directorId)
                    .SendAsync("ReceiveNotification", new
                    {
                        Title = "Заявка одобрена помощником",
                        Message = "Заявка одобрена помощником, ожидает вашего решения.",
                        ApplicationId = app.Id
                    });
            }
        }
        else
        {
            await hub.Clients.User(app.CreatedById)
                .SendAsync("ReceiveNotification", new
                {
                    Title = "Заявка отклонена",
                    Message = "Ваша заявка отклонена помощником директора.",
                    ApplicationId = app.Id
                });
        }

        return app;
    }

    public async Task<Application> DirectorReviewAsync(string userId, Guid appId, DirectorReviewDto dto)
    {
        var app = await db.Applications.Include(a => a.Company).FirstOrDefaultAsync(a => a.Id == appId);
        if (app == null)
            throw new Exception("Заявка не найдена");

        var companyUser = await db.CompanyUsers
            .FirstOrDefaultAsync(x => x.UserId == userId && x.CompanyId == app.CompanyId);
        if (companyUser == null || companyUser.Role != CompanyRole.Director)
            throw new Exception("Нет прав для рассмотрения директором");

        if (app.Status != ApplicationStatus.AssistantApproved)
            throw new Exception("Заявка должна быть одобрена помощником для рассмотрения директором");

        app.DirectorReviewedAt = DateTime.UtcNow;
        app.DirectorComment = dto.Comment;
        app.Status = dto.Approve ? ApplicationStatus.DirectorApproved : ApplicationStatus.DirectorRejected;

        await db.SaveChangesAsync();

        var msg = dto.Approve
            ? "Ваша заявка одобрена директором"
            : "Ваша заявка отклонена директором";
        await hub.Clients.User(app.CreatedById)
            .SendAsync("ReceiveNotification", new
            {
                Title = "Рассмотрение заявки директором",
                Message = msg,
                ApplicationId = app.Id
            });

        return app;
    }
    
    public async Task<ApplicationStatsDto> GetUserApplicationsStatsAsync(string userId)
    {
        var userCompanyIds = await db.CompanyUsers
            .Where(cu => cu.UserId == userId)
            .Select(cu => cu.CompanyId)
            .ToListAsync();

        var companies = userCompanyIds.Count;

        var applications = await db.Applications
            .Where(a => userCompanyIds.Contains(a.CompanyId) && a.CreatedById == userId)
            .ToListAsync();

        var applicationsTotal = applications.Count;
        var applicationsDone = applications.Count(a =>
                a.Status is ApplicationStatus.DirectorApproved or ApplicationStatus.AssistantApproved 
        );
        var applicationsPending = applications.Count(a =>
                a.Status is ApplicationStatus.New or ApplicationStatus.AssistantApproved 
        );

        return new ApplicationStatsDto
        {
            Companies = companies,
            ApplicationsTotal = applicationsTotal,
            ApplicationsDone = applicationsDone,
            ApplicationsPending = applicationsPending
        };
    }
}