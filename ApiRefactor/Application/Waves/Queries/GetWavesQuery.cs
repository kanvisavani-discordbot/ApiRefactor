using ApiRefactor.Domain.Common;
using ApiRefactor.Domain.Entities;
using MediatR;

namespace ApiRefactor.Application.Waves.Queries;

public sealed record GetWavesQuery(int Page, int PageSize)
    : IRequest<PagedResult<Wave>>;
