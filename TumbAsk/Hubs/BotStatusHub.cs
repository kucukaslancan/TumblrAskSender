using Microsoft.AspNetCore.SignalR;

namespace TumbAsk.Hubs
{
    public class BotStatusHub : Hub
    {
        public async Task SendStatusUpdate(string botId, string status)
        {
            await Clients.All.SendAsync("ReceiveStatusUpdate", botId, status);
        }
    }
}
