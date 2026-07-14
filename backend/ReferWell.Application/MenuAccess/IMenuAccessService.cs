using ReferWell.Application.Common.Models;

namespace ReferWell.Application.MenuAccess;

public interface IMenuAccessService
{
    Task<AppResult> GetAsync(CancellationToken ct = default);
    Task<AppResult> UpdateAsync(List<RoleMenuAccessDto> req, CancellationToken ct = default);
}
