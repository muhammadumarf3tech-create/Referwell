namespace ReferWell.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid UserId { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
