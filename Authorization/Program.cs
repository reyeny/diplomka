using System.Text;
using System.Text.Json.Serialization;
using Authorization.Context;
using Authorization.Helpers;
using Authorization.Models;
using Authorization.Services.ApplicationService;
using Authorization.Services.AuthenticationServices;
using Authorization.Services.Captcha;
using Authorization.Services.Captcha.Interface;
using Authorization.Services.CompanyService;
using Authorization.Services.EmailSenderConfirm;
using Authorization.Services.EmailSenderConfirm.Interfaces;
using Authorization.Services.InvitationService;
using Authorization.Services.TaskService;
using Authorization.Services.UserServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Authorization.Models.User;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:9000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var connectionString = configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<UnchainMeDbContext>(opt =>
    opt.UseNpgsql(connectionString));

builder.Services.AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<UnchainMeDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken             = true;
        options.RequireHttpsMetadata  = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = configuration["JWT:ValidIssuer"],
            ValidAudience            = configuration["JWT:ValidAudience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!)),
            RoleClaimType            = System.Security.Claims.ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/notifications"))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var roles = context.Principal!.Claims
                    .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                    .Select(c => c.Value);
                Console.WriteLine("Роли в токене: " + string.Join(", ", roles));
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.Configure<RecaptchaSettings>(
    configuration.GetSection("Recaptcha"));

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddHttpClient<IRecaptchaService, RecaptchaService>();

builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();

builder.Services.AddSignalR();

var botToken = configuration["Telegram:BotToken"]!;
if (string.IsNullOrWhiteSpace(botToken))
    throw new Exception("Токен Telegram бота не задан!");

var botClient = new TelegramBotClient(botToken);
builder.Services.AddSingleton<ITelegramBotClient>(botClient);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    var dbContext = scope.ServiceProvider.GetRequiredService<UnchainMeDbContext>();


    dbContext.Database.Migrate(); 

    await EnsureRoles(services);
    //await EnsureRolesAndUsersAsync(services);
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHub<NotificationHub>("/notifications");

using var cts = new CancellationTokenSource();
botClient.StartReceiving(
    HandleUpdateAsync,
    HandlePollingErrorAsync,
    new ReceiverOptions { AllowedUpdates = [] },
    cts.Token
);

Console.WriteLine("Telegram бот запущен...");
app.Run();
return;


async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<UnchainMeDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    switch (update.Type)
    {
        case UpdateType.Message when update.Message!.Type == MessageType.Text:
        {
            var chatId = update.Message.Chat.Id;
            var emailText = update.Message.Text!.Trim();
            var user = await userManager.FindByEmailAsync(emailText);
            if (user != null)
            {
                user.TelegramChatId = chatId.ToString();
                user.HasUsed2FA = false; 
                await userManager.UpdateAsync(user);

                await bot.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Аккаунт {emailText} успешно привязан! Теперь подтвердите почту на сайте."
                );
            }
            else
            {
                await bot.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Пользователь с указанным email не найден."
                );
            }
            return;
        }
        case UpdateType.CallbackQuery when update.CallbackQuery!.Data != null:
        {
            var data = update.CallbackQuery.Data!;
            if (data.StartsWith("approve:") || data.StartsWith("reject:"))
            {
                var parts = data.Split(':', 2);
                var action = parts[0];
                if (Guid.TryParse(parts[1], out var reqId))
                {
                    var loginRequest = await dbContext.LoginRequests.FindAsync(reqId);
                    var chatId = update.CallbackQuery.Message!.Chat.Id;

                    if (loginRequest == null || DateTime.UtcNow > loginRequest.ExpiresAt)
                    {
                        await bot.SendTextMessageAsync(chatId, "Запрос на вход не найден или уже просрочен.");
                        return;
                    }

                    if (action == "approve")
                    {
                        loginRequest.IsApproved = true;
                        await dbContext.SaveChangesAsync(ct);

                        await bot.SendTextMessageAsync(chatId, "Вход подтверждён! Возвращайтесь на сайт.");
                    }
                    else
                    {
                        dbContext.LoginRequests.Remove(loginRequest);
                        await dbContext.SaveChangesAsync();

                        await bot.SendTextMessageAsync(chatId, "Вход отклонён.");
                    }
                }
            }

            break;
        }
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
{
    Console.WriteLine(exception is ApiRequestException apiEx
        ? $"Telegram API Error:\n[{apiEx.ErrorCode}] {apiEx.Message}"
        : exception.ToString());
    return Task.CompletedTask;
}

static async Task EnsureRoles(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roleNames = ["Employee", "Manager", "Assistant", "Director", "Admin", "enAdmin"];

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));
    }
}

static async Task EnsureRolesAndUsersAsync(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<User>>(); 

    var rolesWithUsers = new (string Role, string Email)[]
    {
        ("Employee", "employee@gmail.com"),
        ("Manager", "manager@gmail.com"),
        ("Assistant", "assistant@gmail.com"),
        ("Director", "director@gmail.com"),
        ("Admin", "admin@gmail.com"),
        ("enAdmin", "enadmin@gmail.com")
        
    };

    const string defaultPassword = "Qwerty#01";

    foreach (var (roleName, email) in rolesWithUsers)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));
        

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new User { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, defaultPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, roleName);
            
        }
        else if (!await userManager.IsInRoleAsync(user, roleName))
            await userManager.AddToRoleAsync(user, roleName);
        
    }
}