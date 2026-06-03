using ApiRefactor.Domain.Entities;
using ApiRefactor.Domain.Interfaces;
using MediatR;

namespace ApiRefactor.Application.Waves.Queries;

public sealed class GetWaveByIdQueryHandler : IRequestHandler<GetWaveByIdQuery, Wave?>
{
    private readonly IWaveRepository _repository;
    private readonly ILogger<GetWaveByIdQueryHandler> _logger;

    public GetWaveByIdQueryHandler(
        IWaveRepository repository,
        ILogger<GetWaveByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Wave?> Handle(
        GetWaveByIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetWaveByIdQuery for wave {WaveId}", request.Id);
        return await _repository.GetByIdAsync(request.Id, cancellationToken);
    }
}
