// src/SocialAutomation.Worker/Program.cs

using Serilog;
using SocialAutomation.Application.AI.Services;
using SocialAutomation.Application.Instagram.Services;
using SocialAutomation.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();

// Infraestrutura e Repositórios
builder.Services.AddInfrastructure(builder.Configuration);

// Application Services para o Worker
builder.Services.AddScoped<InstagramWebhookProcessor>();
builder.Services.AddScoped<AiAgentService>();
builder.Services.AddScoped<KnowledgeService>();
builder.Services.AddScoped<AiConversationService>();

// Workers
builder.Services.AddHostedService<SocialAutomation.Worker.Workers.HealthCheckWorker>();
builder.Services.AddHostedService<SocialAutomation.Worker.Workers.InstagramWebhookWorker>();
builder.Services.AddScoped<SocialAutomation.Application.Content.Services.ContentService>();
builder.Services.AddHostedService<SocialAutomation.Worker.Workers.ContentPublisherWorker>();

var host = builder.Build();

Log.Information("SocialAutomation Worker iniciado.");
host.Run();