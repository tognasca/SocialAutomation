// src/SocialAutomation.Infrastructure/Database/DbConnectionFactory.cs

using Npgsql;
using System.Data;

namespace SocialAutomation.Infrastructure.Database;

public class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}