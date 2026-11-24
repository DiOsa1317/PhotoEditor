using Microsoft.AspNetCore.SignalR;

namespace Notifications;

public class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var sessionId = Context.GetHttpContext()?.Request.Query["sessionId"].ToString();
        if (!string.IsNullOrEmpty(sessionId)) await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

        await base.OnConnectedAsync();
    }
}