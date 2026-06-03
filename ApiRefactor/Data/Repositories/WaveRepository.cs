using ApiRefactor.Domain.Common;
using ApiRefactor.Domain.Entities;
using ApiRefactor.Domain.Interfaces;
using Dapper;

namespace ApiRefactor.Data.Repositories;

public sealed class WaveRepository : IWaveRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<WaveRepository> _logger;

    public WaveRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<WaveRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<PagedResult<Wave>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching waves page {Page} with page size {PageSize}", page, pageSize);

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        // Single round-trip: fetch count and page together using multi-query
        const string sql = """
            SELECT COUNT(*) FROM waves;
            SELECT id, name, wavedate FROM waves
            ORDER BY wavedate DESC
            LIMIT @PageSize OFFSET @Offset;
            """;

        using var multi = await connection.QueryMultipleAsync(
            sql,
            new { PageSize = pageSize, Offset = (page - 1) * pageSize });

        var totalCount = await multi.ReadSingleAsync<int>();
        var rows = (await multi.ReadAsync<WaveRow>()).ToList();

        var items = rows.Select(MapToDomain).ToList();

        return new PagedResult<Wave>(items, page, pageSize, totalCount);
    }

    public async Task<Wave?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching wave {WaveId}", id);

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        var row = await connection.QuerySingleOrDefaultAsync<WaveRow>(
            "SELECT id, name, wavedate FROM waves WHERE id = @Id",
            new { Id = id.ToString() });

        return row is null ? null : MapToDomain(row);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM waves WHERE id = @Id",
            new { Id = id.ToString() });

        return count > 0;
    }

    public async Task AddAsync(Wave wave, CancellationToken ct = default)
    {
        _logger.LogDebug("Inserting wave {WaveId}", wave.Id);

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        await connection.ExecuteAsync(
            """
            INSERT INTO waves (id, name, wavedate)
            VALUES (@Id, @Name, @WaveDate)
            """,
            new
            {
                Id = wave.Id.ToString(),
                wave.Name,
                WaveDate = wave.WaveDate.ToString("o")
            });
    }

    public async Task UpdateAsync(Wave wave, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating wave {WaveId}", wave.Id);

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        await connection.ExecuteAsync(
            """
            UPDATE waves
            SET name = @Name, wavedate = @WaveDate
            WHERE id = @Id
            """,
            new
            {
                Id = wave.Id.ToString(),
                wave.Name,
                WaveDate = wave.WaveDate.ToString("o")
            });
    }

    private static Wave MapToDomain(WaveRow row) =>
        Wave.Reconstitute(
            Guid.Parse(row.Id),
            row.Name,
            DateTime.Parse(row.WaveDate));

    // Flat DTO used only within the repository to avoid Dapper reflection on the domain entity
    private sealed record WaveRow(string Id, string Name, string WaveDate);
}
