using Microsoft.AspNetCore.Http;
using ReferWell.Domain.Entities;
using ReferWell.Infrastructure.Data;

namespace ReferWell.Infrastructure.Services;

public class SecurityAuditService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public SecurityAuditService(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(
        string action,
        Guid? actorUserId = null,
        string? actorEmail = null,
        string? details = null,
        CancellationToken ct = default)
    {
        var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
        _db.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Action = action,
            ActorUserId = actorUserId,
            ActorEmail = actorEmail,
            Details = details,
            IpAddress = ip
        });
        await _db.SaveChangesAsync(ct);
    }
}
