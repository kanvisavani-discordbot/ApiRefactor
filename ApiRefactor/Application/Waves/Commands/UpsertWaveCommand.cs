using MediatR;

namespace ApiRefactor.Application.Waves.Commands;

public sealed record UpsertWaveCommand(
    Guid? Id,
    string Name,
    DateTime? WaveDate) : IRequest<Guid>;
