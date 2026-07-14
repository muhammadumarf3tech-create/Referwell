namespace ReferWell.Application.Common.Interfaces;

public interface IMenuAccessChecker
{
    Task<bool> HasMenuAccessAsync(IEnumerable<string> roleNames, string menuItem, CancellationToken ct = default);
}
