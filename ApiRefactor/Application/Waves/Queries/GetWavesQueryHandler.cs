using ApiRefactor.Domain.Common;
using ApiRefactor.Domain.Entities;
using ApiRefactor.Domain.Interfaces;
using MediatR;

namespace ApiRefactor.Application.Waves.Queries;

public sealed class GetWavesQueryHandler
    : IRequestHandler<GetWavesQuery, PagedResult<Wave>>
{
    private readonly IWaveRepository _repository;
    private readonly ILogger<GetWavesQueryHandler> _logger;

    public GetWavesQueryHandler(
        IWaveRepository repository,
        ILogger<GetWavesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PagedResult<Wave>> Handle(
        GetWavesQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GetWavesQuery: page={Page}, pageSize={PageSize}",
            request.Page,
            request.PageSize);

        return await _repository.GetPagedAsync(
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
