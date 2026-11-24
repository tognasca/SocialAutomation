
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

// ==========================================================
// 1. SERILOG
// ==========================================================

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ==========================================================
// 2. INFRASTRUCTURE
// ==========================================================

builder.Services.AddInfrastructure(builder.Configuration);

// ==========================================================
// 3. APPLICATION SERVICES
// ==========================================================

builder.Services.AddScoped<AuthService>(sp =>
{
    var refreshDays = int.Parse(
        builder.Configuration["Jwt:RefreshExpirationDays"] ?? "30");

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

// Instagram
builder.Services.AddScoped<InstagramAccountService>();
builder.Services.AddScoped<InstagramMessageService>();
builder.Services.AddScoped<InstagramWebhookProcessor>();

// CRM
builder.Services.AddScoped<
    SocialAutomation.Application.CRM.Services.LeadService>();

builder.Services.AddScoped<
    SocialAutomation.Application.CRM.Services.PipelineService>();

builder.Services.AddScoped<
    SocialAutomation.Application.CRM.Services.CrmTaskService>();

builder.Services.AddScoped<
    SocialAutomation.Application.CRM.Services.TagService>();

// Inbox
builder.Services.AddScoped<
    SocialAutomation.Application.Inbox.Services.InboxService>();

// Inteligencia Artificial
builder.Services.AddScoped<
    SocialAutomation.Application.AI.Services.AiAgentService>();

builder.Services.AddScoped<
    SocialAutomation.Application.AI.Services.KnowledgeService>();

builder.Services.AddScoped<
    SocialAutomation.Application.AI.Services.AiConversationService>();

// Automacao
builder.Services.AddScoped<
    SocialAutomation.Application.Automation.Services.AutomationService>();

builder.Services.AddScoped<
    SocialAutomation.Application.Automation.Services.CampaignService>();

builder.Services.AddScoped<
    SocialAutomation.Application.Automation.Services.AutomationEngine>();

// Conteudo
builder.Services.AddScoped<
    SocialAutomation.Application.Content.Services.ContentService>();

builder.Services.AddScoped<
    SocialAutomation.Application.Content.Services.ContentGeneratorService>();

// Analytics
builder.Services.AddScoped<
    SocialAutomation.Application.Analytics.Services.AnalyticsService>();

// ==========================================================
// 4. FLUENT VALIDATION
// ==========================================================

builder.Services.AddValidatorsFromAssemblyContaining<
    LoginRequestValidator>();

// ==========================================================
// 5. AUTENTICACAO JWT
// ==========================================================

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException(
        "Jwt:Secret nao foi configurado.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
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

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecret)),

        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ==========================================================
// 6. CONTROLLERS
// ==========================================================

builder.Services.AddControllers();

// ==========================================================
// 7. CORS
// ==========================================================

// Origens obrigatoriamente autorizadas.
// Mesmo que App:AllowedOrigins esteja configurado no Docker,
// essas origens continuarao permitidas.

const string CorsPolicy = "AllowFrontend";

var frontendOrigins = new HashSet<string>(
    StringComparer.OrdinalIgnoreCase)
{
    "https://socialautomation.ademartognasca.com.br",
    "http://localhost:3100",
    "http://localhost:3101"
};

// Origens adicionais configuradas no appsettings ou Docker.
var configuredOrigins =
    builder.Configuration["App:AllowedOrigins"];

if (!string.IsNullOrWhiteSpace(configuredOrigins))
{
    foreach (var origin in configuredOrigins.Split(
        ';',
        StringSplitOptions.RemoveEmptyEntries |
        StringSplitOptions.TrimEntries))
    {
        var normalizedOrigin = origin.TrimEnd('/');

        if (Uri.TryCreate(
            normalizedOrigin,
            UriKind.Absolute,
            out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp ||
             uri.Scheme == Uri.UriSchemeHttps) &&
            string.IsNullOrEmpty(uri.AbsolutePath.Trim('/')) &&
            string.IsNullOrEmpty(uri.Query) &&
            string.IsNullOrEmpty(uri.Fragment))
        {
            frontendOrigins.Add(normalizedOrigin);
        }
        else
        {
            Log.Warning(
                "Origem CORS ignorada por formato invalido: {Origin}",
                normalizedOrigin);
        }
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy
            .WithOrigins(frontendOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ==========================================================
// 8. SWAGGER
// ==========================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SocialAutomation API",
        Version = "v1"
    });

    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header
        });

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

// ==========================================================
// 9. BUILD
// ==========================================================

var app = builder.Build();

// ==========================================================
// 10. MIGRATIONS SQL
// ==========================================================

using (var scope = app.Services.CreateScope())
{
    try
    {
        var migrationRunner = scope.ServiceProvider
            .GetRequiredService<MigrationRunner>();

        var contentRoot = app.Environment.ContentRootPath;

        var candidatePaths = new[]
        {
            Environment.GetEnvironmentVariable("MIGRATIONS_PATH"),

            Path.Combine(
                contentRoot,
                "..",
                "SocialAutomation.Infrastructure",
                "Database",
                "migrations"),

            Path.Combine(
                contentRoot,
                "..",
                "..",
                "database",
                "migrations"),

            Path.Combine(
                AppContext.BaseDirectory,
                "migrations"),

            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "database",
                "migrations")
        };

        string? selectedPath = null;

        foreach (var path in candidatePaths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            if (!Directory.Exists(path))
                continue;

            var files = Directory.GetFiles(path, "*.sql");

            if (files.Any(f => new FileInfo(f).Length > 0))
            {
                selectedPath = path;
                break;
            }
        }

        if (selectedPath != null)
        {
            Log.Information(
                "Pasta de migrations localizada: {Path}",
                selectedPath);

            await migrationRunner.RunAsync(selectedPath);
        }
        else
        {
            Log.Error(
                "Nenhuma pasta de migrations com arquivos SQL validos foi localizada.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(
            ex,
            "Erro ao aplicar migrations no banco.");
    }
}

// ==========================================================
// 11. PIPELINE HTTP
// ==========================================================

// Correlation ID
app.UseMiddleware<CorrelationIdMiddleware>();

// Tratamento global de erros
app.UseMiddleware<ErrorHandlingMiddleware>();

// Logging de requisicoes
app.UseMiddleware<RequestLoggingMiddleware>();

// Swagger somente em desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Roteamento
app.UseRouting();

// CORS antes da autenticacao
app.UseCors(CorsPolicy);

// Autenticacao e autorizacao
app.UseAuthentication();
app.UseAuthorization();

// Resolucao do tenant
app.UseMiddleware<TenantMiddleware>();

// Controllers
app.MapControllers();

// ==========================================================
// 12. INICIALIZACAO
// ==========================================================

Log.Information(
    "SocialAutomation API iniciada com sucesso.");

app.Run();

public partial class Program { }
