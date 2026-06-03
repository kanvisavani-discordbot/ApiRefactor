using ApiRefactor.Application.Waves.Commands;
using ApiRefactor.Domain.Entities;
using ApiRefactor.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ApiRefactor.Tests.Application;

public sealed class UpsertWaveCommandHandlerTests
{
    private readonly IWaveRepository _repository = Substitute.For<IWaveRepository>();
    private readonly UpsertWaveCommandHandler _sut;

    public UpsertWaveCommandHandlerTests()
    {
        _sut = new UpsertWaveCommandHandler(
            _repository,
            NullLogger<UpsertWaveCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenNoIdProvided_ShouldInsertAndReturnNewId()
    {
        var command = new UpsertWaveCommand(null, "NewWave", null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<Wave>(w => w.Name == "NewWave"),
            Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().UpdateAsync(
            Arg.Any<Wave>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenIdProvidedAndNotExists_ShouldInsertWithSuppliedId()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Wave?)null);

        var command = new UpsertWaveCommand(id, "NewWave", null);
        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().Be(id);
        await _repository.Received(1).AddAsync(
            Arg.Is<Wave>(w => w.Id == id && w.Name == "NewWave"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenIdProvidedAndExists_ShouldUpdate()
    {
        var id = Guid.NewGuid();
        var existing = Wave.Reconstitute(
            id,
            "OldName",
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(existing);

        var command = new UpsertWaveCommand(id, "UpdatedName", null);
        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().Be(id);
        await _repository.Received(1).UpdateAsync(
            Arg.Is<Wave>(w => w.Name == "UpdatedName"),
            Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddAsync(
            Arg.Any<Wave>(),
            Arg.Any<CancellationToken>());
    }
}
