using Dapper;

namespace ApiRefactor.Data;

public sealed class DatabaseInitialiser
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseInitialiser> _logger;

    public DatabaseInitialiser(
        IDbConnectionFactory connectionFactory,
        ILogger<DatabaseInitialiser> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InitialiseAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Initialising database schema");

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS waves (
                id       TEXT    NOT NULL PRIMARY KEY,
                name     TEXT    NOT NULL,
                wavedate TEXT    NOT NULL
            );
            """);

        _logger.LogInformation("Database schema ready");
    }
}
