using ApiRefactor.Domain.Entities;
using ApiRefactor.Domain.Interfaces;
using MediatR;

namespace ApiRefactor.Application.Waves.Commands;

public sealed class UpsertWaveCommandHandler : IRequestHandler<UpsertWaveCommand, Guid>
{
    private readonly IWaveRepository _repository;
    private readonly ILogger<UpsertWaveCommandHandler> _logger;

    public UpsertWaveCommandHandler(
        IWaveRepository repository,
        ILogger<UpsertWaveCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Guid> Handle(
        UpsertWaveCommand request,
        CancellationToken cancellationToken)
    {
        // If no ID is provided, this is always an insert
        if (request.Id is null)
        {
            var newWave = Wave.Create(request.Name);
            if (request.WaveDate.HasValue)
                newWave.UpdateWaveDate(request.WaveDate.Value);

            await _repository.AddAsync(newWave, cancellationToken);

            _logger.LogInformation("Created wave {WaveId}", newWave.Id);
            return newWave.Id;
        }

        var existing = await _repository.GetByIdAsync(request.Id.Value, cancellationToken);

        if (existing is null)
        {
            // Insert with caller-supplied ID (idempotent create)
            var wave = Wave.Reconstitute(
                request.Id.Value,
                request.Name,
                request.WaveDate ?? DateTime.UtcNow);

            await _repository.AddAsync(wave, cancellationToken);

            _logger.LogInformation("Created wave {WaveId} with supplied ID", wave.Id);
            return wave.Id;
        }

        // Update existing
        existing.UpdateName(request.Name);
        if (request.WaveDate.HasValue)
            existing.UpdateWaveDate(request.WaveDate.Value);

        await _repository.UpdateAsync(existing, cancellationToken);

        _logger.LogInformation("Updated wave {WaveId}", existing.Id);
        return existing.Id;
    }
}
