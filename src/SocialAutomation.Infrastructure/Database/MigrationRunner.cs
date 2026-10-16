// src/SocialAutomation.Infrastructure/Database/MigrationRunner.cs

using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace SocialAutomation.Infrastructure.Database;

public class MigrationRunner
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<MigrationRunner> _logger;
    private readonly string _connectionString;

    public MigrationRunner(
        DbConnectionFactory connectionFactory,
        ILogger<MigrationRunner> logger,
        IConfiguration configuration)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? configuration.GetConnectionString("Postgres")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:PostgreSQL"]
            ?? configuration["ConnectionStrings:Postgres"]
            ?? string.Empty;
    }

    public async Task RunAsync(string migrationsPath)
    {
        // 1. Garante que o banco de dados existe
        await EnsureDatabaseCreatedAsync();

        // 2. Conecta no banco da aplicação
        using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await EnsureMigrationTableAsync(connection);

        if (!Directory.Exists(migrationsPath))
        {
            _logger.LogWarning("Diretório de migrations não encontrado: {Path}", migrationsPath);
            return;
        }

        var files = Directory.GetFiles(migrationsPath, "*.sql")
            .OrderBy(f => f)
            .ToList();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var version = fileName.Split('_')[0];
            var content = (await File.ReadAllTextAsync(file)).Trim();

            // Se o arquivo estiver vazio, ignora para não dar erro no Npgsql
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Migration {FileName} está vazia. Pulando.", fileName);
                continue;
            }

            var checksum = ComputeChecksum(content);

            var alreadyRan = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT version FROM schema_migrations WHERE version = @Version",
                new { Version = version });

            if (alreadyRan != null)
            {
                _logger.LogDebug("Migration {FileName} já executada, ignorando.", fileName);
                continue;
            }

            _logger.LogInformation("Aplicando migration: {FileName}", fileName);

            try
            {
                // Executa o script SQL diretamente via NpgsqlCommand
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = content;
                    await cmd.ExecuteNonQueryAsync();
                }

                // Registra que a migration foi aplicada
                await connection.ExecuteAsync(
                    "INSERT INTO schema_migrations (version, name, checksum) VALUES (@Version, @Name, @Checksum)",
                    new { Version = version, Name = fileName, Checksum = checksum });

                _logger.LogInformation("Migration {FileName} aplicada com sucesso.", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao aplicar migration {FileName}", fileName);
                throw;
            }
        }
    }

    private async Task EnsureDatabaseCreatedAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString)) return;

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(_connectionString);
            var targetDatabase = builder.Database;

            if (string.IsNullOrWhiteSpace(targetDatabase)) return;

            builder.Database = "postgres";
            using var masterConnection = new NpgsqlConnection(builder.ConnectionString);
            await masterConnection.OpenAsync();

            var exists = await masterConnection.QueryFirstOrDefaultAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname = @DbName)",
                new { DbName = targetDatabase });

            if (!exists)
            {
                _logger.LogInformation("Criando banco de dados '{DbName}' automaticamente...", targetDatabase);
                await masterConnection.ExecuteAsync($"CREATE DATABASE \"{targetDatabase}\"");
                _logger.LogInformation("Banco de dados '{DbName}' criado com sucesso.", targetDatabase);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível verificar/criar a base automaticamente através do banco 'postgres'.");
        }
    }

    private static async Task EnsureMigrationTableAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version VARCHAR(20) NOT NULL PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                executed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                checksum VARCHAR(64)
            )");
    }

    private static string ComputeChecksum(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}