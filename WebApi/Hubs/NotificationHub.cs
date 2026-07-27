using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace WebApi.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // ICurrentUserService.UserId ile aynı claim sırası: SignalRNotificationService.SendNotificationToUserAsync
            // bu değeri grup adı olarak kullanıyor, ikisi eşleşmezse kullanıcıya özel bildirim asla ulaşmaz.
            var userId = Context.User?.FindFirst("uid")?.Value
                ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }

            // Adminler için "admin-dashboard" grubu
            var httpContext = Context.GetHttpContext();
            if (httpContext != null && httpContext.Request.Query["group"] == "admin")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "AdminDashboard");
            }

            await base.OnConnectedAsync();
        }

        // İstemciler (client'lar) 'ReceiveNotification' metodunu dinleyecek.
        // Bu metodu biz doğrudan çağırmayacağız, IHubContext üzerinden tetikleyeceğiz.
    }
}