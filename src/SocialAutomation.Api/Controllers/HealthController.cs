// src/SocialAutomation.Api/Controllers/HealthController.cs

using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Infrastructure.Database;
using StackExchange.Redis;
using Dapper;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly IConnectionMultiplexer _redis;

    public HealthController(DbConnectionFactory connectionFactory, IConnectionMultiplexer redis)
    {
        _connectionFactory = connectionFactory;
        _redis = redis;
    }

    [HttpGet]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "SocialAutomation.Api"
        });
    }

    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        var checks = new Dictionary<string, string>();

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.QueryFirstOrDefaultAsync<int>("SELECT 1");
            checks["postgresql"] = "healthy";
        }
        catch (Exception ex)
        {
            checks["postgresql"] = $"unhealthy: {ex.Message}";
        }

        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync();
            checks["redis"] = "healthy";
        }
        catch (Exception ex)
        {
            checks["redis"] = $"unhealthy: {ex.Message}";
        }

        var allHealthy = checks.Values.All(v => v == "healthy");

        return allHealthy
            ? Ok(new { status = "ready", checks, timestamp = DateTime.UtcNow })
            : StatusCode(503, new { status = "not ready", checks, timestamp = DateTime.UtcNow });
    }
}