// Program.cs
using System.Text;
using Authorization.Context;
using Authorization.Models;
using Authorization.Services.AuthenticationServices;
using Authorization.Services.Captcha;
using Authorization.Services.Captcha.Interface;
using Authorization.Services.EmailSenderConfirm;
using Authorization.Services.EmailSenderConfirm.Interfaces;
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

// 1) CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:9000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// 2) DbContext
var connectionString = configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<UnchainMeDbContext>(opt =>
    opt.UseNpgsql(connectionString));

// 3) Identity
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

// 4) JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["JWT:ValidIssuer"],
        ValidAudience = configuration["JWT:ValidAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!))
    };
});

// 5) Controllers & Authorization
builder.Services.AddAuthorization();
builder.Services.AddControllers();

// 6) reCAPTCHA
builder.Services.Configure<RecaptchaSettings>(
    configuration.GetSection("Recaptcha"));

// 7) DI for our services
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddHttpClient<IRecaptchaService, RecaptchaService>();

// 8) Telegram BotClient
var botToken = configuration["Telegram:BotToken"]!;
if (string.IsNullOrWhiteSpace(botToken))
    throw new Exception("Токен Telegram бота не задан!");

var botClient = new TelegramBotClient(botToken);
builder.Services.AddSingleton<ITelegramBotClient>(botClient);

// 9) Swagger (development)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

// 10) Запускаем Long Polling бота
using var cts = new CancellationTokenSource();
botClient.StartReceiving(
    HandleUpdateAsync,
    HandlePollingErrorAsync,
    new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
    cts.Token
);

Console.WriteLine("Telegram бот запущен...");
app.Run();


// ===== Обработчик входящих апдейтов (сообщения + CallbackQuery) =====
async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<UnchainMeDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    // 1) Если пользователь сообщил боту свой Email (для привязки TelegramChatId)
    if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
    {
        var chatId = update.Message.Chat.Id;
        var emailText = update.Message.Text.Trim();
        var user = await userManager.FindByEmailAsync(emailText);
        if (user != null)
        {
            user.TelegramChatId = chatId.ToString();
            user.HasUsed2FA = false; // при следующем ConfirmEmail будет сгенерирован код
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

    // 2) Если пользователь нажал inline-кнопку “Подтвердить вход” или “Отклонить вход”
    if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery!.Data != null)
    {
        var data = update.CallbackQuery.Data!;
        // Формат data = "approve:<RequestId>" или "reject:<RequestId>"
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
                    await bot.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Запрос на вход не найден или уже просрочен."
                    );
                    return;
                }

                if (action == "approve")
                {
                    loginRequest.IsApproved = true;
                    await dbContext.SaveChangesAsync();

                    await bot.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вход подтверждён! Возвращайтесь на сайт."
                    );
                }
                else // “reject”
                {
                    dbContext.LoginRequests.Remove(loginRequest);
                    await dbContext.SaveChangesAsync();

                    await bot.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вход отклонён."
                    );
                }
            }
        }
    }
}


// ===== Обработчик ошибок Long Polling =====
Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
{
    Console.WriteLine(exception is ApiRequestException apiEx
        ? $"Telegram API Error:\n[{apiEx.ErrorCode}] {apiEx.Message}"
        : exception.ToString());
    return Task.CompletedTask;
}
