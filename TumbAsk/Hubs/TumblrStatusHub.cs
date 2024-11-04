using Microsoft.AspNetCore.SignalR;

namespace TumbAsk.Hubs
{
    public class TumblrStatusHub : Hub
    {
        public async Task SendStatusUpdate(string message)
        {
            await Clients.All.SendAsync("ReceiveStatusUpdate", message);
        }
    }
}
