// src/SocialAutomation.Api/Program.cs

using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SocialAutomation.Api.Middlewares;
using SocialAutomation.Application.Auth.Services;
using SocialAutomation.Application.Auth.Validators;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Application.Instagram.Services;
using SocialAutomation.Application.Tenants.Services;
using SocialAutomation.Application.Users.Services;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;
using SocialAutomation.Infrastructure;
using SocialAutomation.Infrastructure.Database;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Application Services
builder.Services.AddScoped<AuthService>(sp =>
{
    var refreshDays = int.Parse(builder.Configuration["Jwt:RefreshExpirationDays"] ?? "30");
    return new AuthService(
        sp.GetRequiredService<IUserRepository>(),
        sp.GetRequiredService<ITenantRepository>(),
        sp.GetRequiredService<IRoleRepository>(),
        sp.GetRequiredService<IPermissionRepository>(),
        sp.GetRequiredService<IRefreshTokenRepository>(),
        sp.GetRequiredService<IAuditLogRepository>(),
        sp.GetRequiredService<IPasswordHasher>(),
        sp.GetRequiredService<ITokenService>(),
        sp.GetRequiredService<IDateTimeProvider>(),
        refreshDays);
});

builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<InstagramAccountService>();
builder.Services.AddScoped<InstagramMessageService>();
builder.Services.AddScoped<SocialAutomation.Application.CRM.Services.LeadService>();
builder.Services.AddScoped<SocialAutomation.Application.CRM.Services.PipelineService>();
builder.Services.AddScoped<SocialAutomation.Application.CRM.Services.CrmTaskService>();
builder.Services.AddScoped<SocialAutomation.Application.CRM.Services.TagService>();
builder.Services.AddScoped<SocialAutomation.Application.Inbox.Services.InboxService>();
builder.Services.AddScoped<InstagramWebhookProcessor>();
builder.Services.AddScoped<SocialAutomation.Application.AI.Services.AiAgentService>();
builder.Services.AddScoped<SocialAutomation.Application.AI.Services.KnowledgeService>();
builder.Services.AddScoped<SocialAutomation.Application.AI.Services.AiConversationService>();
builder.Services.AddScoped<SocialAutomation.Application.Automation.Services.AutomationService>();
builder.Services.AddScoped<SocialAutomation.Application.Automation.Services.CampaignService>();
builder.Services.AddScoped<SocialAutomation.Application.Automation.Services.AutomationEngine>();
builder.Services.AddScoped<SocialAutomation.Application.Content.Services.ContentService>();
builder.Services.AddScoped<SocialAutomation.Application.Content.Services.ContentGeneratorService>();
builder.Services.AddScoped<SocialAutomation.Application.Analytics.Services.AnalyticsService>();




// 4. FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// 5. JWT
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
    throw new InvalidOperationException("Configure Jwt:Secret por User Secrets ou variável de ambiente (mínimo de 32 caracteres).");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// 6. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["App:FrontendUrl"] ?? "http://localhost:3100")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 7. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SocialAutomation API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 8. Execução Inteligente das Migrations
using (var scope = app.Services.CreateScope())
{
    try
    {
        var migrationRunner = scope.ServiceProvider.GetRequiredService<MigrationRunner>();
        var contentRoot = app.Environment.ContentRootPath;

        // Lista de locais prováveis para encontrar os arquivos SQL de migration
        var candidatePaths = new[]
        {
            Environment.GetEnvironmentVariable("MIGRATIONS_PATH"),
            Path.Combine(contentRoot, "..", "SocialAutomation.Infrastructure", "Database", "migrations"),
            Path.Combine(contentRoot, "..", "..", "database", "migrations"),
            Path.Combine(AppContext.BaseDirectory, "migrations"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "database", "migrations")
        };

        string? selectedPath = null;
        foreach (var path in candidatePaths)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                // Verifica se a pasta contém arquivos SQL com conteúdo
                var files = Directory.GetFiles(path, "*.sql");
                if (files.Any(f => new FileInfo(f).Length > 0))
                {
                    selectedPath = path;
                    break;
                }
            }
        }

        if (selectedPath != null)
        {
            Log.Information("Pasta de migrations localizada: {Path}", selectedPath);
            await migrationRunner.RunAsync(selectedPath);
        }
        else
        {
            Log.Error("Nenhuma pasta de migrations com arquivos SQL válidos foi localizada.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Erro fatal ao aplicar migrations no banco.");
    }
}

// 9. Pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();

app.MapControllers();

Log.Information("SocialAutomation API iniciada com sucesso.");
app.Run();

public partial class Program { }