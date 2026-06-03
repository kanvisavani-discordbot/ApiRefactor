using System.Data;
using ApiRefactor.Data;
using ApiRefactor.Data.Repositories;
using ApiRefactor.Domain.Entities;
using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiRefactor.Tests.Repositories;

/// <summary>
/// Uses an in-memory SQLite database so tests are fast and isolated.
/// Each test class instance gets its own connection, keeping tests independent.
/// </summary>
public sealed class WaveRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WaveRepository _sut;

    public WaveRepositoryTests()
    {
        // Keep the connection open — in-memory SQLite vanishes when last connection closes
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _connection.Execute("""
            CREATE TABLE waves (
                id       TEXT NOT NULL PRIMARY KEY,
                name     TEXT NOT NULL,
                wavedate TEXT NOT NULL
            );
            """);

        var factory = new FixedConnectionFactory(_connection);
        _sut = new WaveRepository(factory, NullLogger<WaveRepository>.Instance);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistWave()
    {
        var wave = Wave.Create("TestWave");
        await _sut.AddAsync(wave);

        var retrieved = await _sut.GetByIdAsync(wave.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(wave.Id);
        retrieved.Name.Should().Be("TestWave");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingWave()
    {
        var wave = Wave.Create("OriginalName");
        await _sut.AddAsync(wave);

        wave.UpdateName("UpdatedName");
        await _sut.UpdateAsync(wave);

        var retrieved = await _sut.GetByIdAsync(wave.Id);
        retrieved!.Name.Should().Be("UpdatedName");
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnCorrectPage()
    {
        for (var i = 1; i <= 5; i++)
            await _sut.AddAsync(Wave.Create($"Wave {i}"));

        var page = await _sut.GetPagedAsync(1, 3);

        page.Items.Should().HaveCount(3);
        page.TotalCount.Should().Be(5);
        page.TotalPages.Should().Be(2);
        page.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalseForUnknownId()
    {
        var exists = await _sut.ExistsAsync(Guid.NewGuid());
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrueAfterInsert()
    {
        var wave = Wave.Create("Wave");
        await _sut.AddAsync(wave);

        var exists = await _sut.ExistsAsync(wave.Id);
        exists.Should().BeTrue();
    }

    public void Dispose() => _connection.Dispose();

    // Adapter to hand the already-open in-memory connection to the repository
    private sealed class FixedConnectionFactory : IDbConnectionFactory
    {
        private readonly IDbConnection _connection;
        public FixedConnectionFactory(IDbConnection connection) => _connection = connection;
        public IDbConnection CreateConnection() => _connection;
    }
}
