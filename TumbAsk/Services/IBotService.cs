using TumbAsk.Models;

namespace TumbAsk.Services
{
    public interface IBotService
    {
        Task<List<Bot>> GetAllBotsAsync();
        Task<bool> AddBotAsync(Bot bot);
        Task RunBotAsync(Bot bot, CancellationToken token);
        Task StartBotAsync(int botId, CancellationToken cancellationToken);
        Task StopBotAsync(int botId, CancellationToken cancellationToken);
        void ScheduleBotTask(Bot bot, CancellationToken token);
        Task PauseBotAsync(int botId, CancellationToken cancellationToken);
        Task DeleteBotAsync(int botId);
        Task StartSendingQuestionsAsync(int botId);

        Task StopQuestion(int botId);
    }
}
