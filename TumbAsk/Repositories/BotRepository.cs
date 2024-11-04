using Microsoft.EntityFrameworkCore;
using TumbAsk.Data;
using TumbAsk.Models;

namespace TumbAsk.Repositories
{
    public class BotRepository : IBotRepository
    {
        private readonly ApplicationDbContext _context;

        public BotRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Bot> GetBotByIdAsync(int id) => await _context.Bots.AsNoTracking().Include(b => b.Logs).FirstOrDefaultAsync(b => b.Id == id);
        public async Task<List<Bot>> GetAllBotsAsync() => await _context.Bots.Include(b => b.Logs).ToListAsync();
        public async Task<BotStatus> GetBotStatusByIdAsync(int id)
        {
            Bot? bot = await _context.Bots.Include(b => b.Logs).FirstOrDefaultAsync(b => b.Id == id);
            return bot?.Status ?? BotStatus.Idle;
        }
        public async Task AddBotAsync(Bot bot)
        {
            _context.Bots.Add(bot);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBotAsync(Bot bot)
        {
            _context.Bots.Update(bot);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBotAsync(int id)
        {
            var bot = await _context.Bots.FindAsync(id);
            if (bot != null)
            {
                _context.Bots.Remove(bot);
                await _context.SaveChangesAsync();
            }
        }
    }
}
