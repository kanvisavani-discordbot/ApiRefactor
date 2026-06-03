using ApiRefactor.Domain.Common;
using ApiRefactor.Domain.Entities;

namespace ApiRefactor.Domain.Interfaces;

public interface IWaveRepository
{
    Task<PagedResult<Wave>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Wave?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Wave wave, CancellationToken ct = default);
    Task UpdateAsync(Wave wave, CancellationToken ct = default);
}
