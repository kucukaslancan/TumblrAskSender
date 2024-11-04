using Microsoft.EntityFrameworkCore;
using TumbAsk.Data;
using TumbAsk.Models;
using TumbAsk.Models.ViewModels;

namespace TumbAsk.Services
{
    public class BotLogService
    {
        private readonly ApplicationDbContext _context;

        public BotLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogResultAsync(int botId, string message, bool success)
        {
            var log = new BotLog
            {
                BotId = botId,
                Message = message,
                Timestamp = DateTime.UtcNow,
                IsSuccess = success
            };

            _context.BotLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<QuestionLogViewModel>> GetQuestionLogsByBotIdAsync(int botId)
        {
            var logs = await _context.BotLogs
                .Where(log => log.BotId == botId)
                .Select(log => new QuestionLogViewModel
                {
                    Username = log.Bot.Username,
                    Message = log.Message, 
                    Timestamp = log.Timestamp,
                    Status = log.IsSuccess
                })
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();

            return logs;
        }

        public async Task DeleteAllLogsAsync()
        {
            _context.BotLogs.RemoveRange(_context.BotLogs);
            await _context.SaveChangesAsync();
        }
    }
}
