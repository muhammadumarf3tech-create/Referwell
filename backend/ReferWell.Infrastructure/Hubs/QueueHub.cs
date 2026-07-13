using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ReferWell.Infrastructure.Hubs;

[Authorize]
public class QueueHub : Hub
{
    public async Task JoinQueue()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "QueueGroup");
    }

    public async Task LeaveQueue()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "QueueGroup");
    }
}
