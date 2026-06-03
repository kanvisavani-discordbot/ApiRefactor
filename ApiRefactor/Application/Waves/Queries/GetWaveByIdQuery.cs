using ApiRefactor.Domain.Entities;
using MediatR;

namespace ApiRefactor.Application.Waves.Queries;

public sealed record GetWaveByIdQuery(Guid Id) : IRequest<Wave?>;
