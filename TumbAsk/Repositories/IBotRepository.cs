using TumbAsk.Models;

namespace TumbAsk.Repositories
{
    public interface IBotRepository
    {
        Task<Bot> GetBotByIdAsync(int id);
        Task<BotStatus> GetBotStatusByIdAsync(int id);
        Task<List<Bot>> GetAllBotsAsync();
        Task AddBotAsync(Bot bot);
        Task UpdateBotAsync(Bot bot);
        Task DeleteBotAsync(int id);
    }
}
