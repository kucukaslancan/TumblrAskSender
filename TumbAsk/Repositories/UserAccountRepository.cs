using Microsoft.EntityFrameworkCore;
using TumbAsk.Data;
using TumbAsk.Models;

namespace TumbAsk.Repositories
{
    public interface IUserAccountRepository
    {
        Task AddUserAccountAsync(UserAccount userAccount);
        Task<bool> UserExistsAsync(string blogName);
        Task<List<UserAccount>> GetAccountsByBotIdAsync(int botId, int pageNumber, int pageSize);
        Task<int> GetTotalAccountsCountAsync(int botId);
        Task<List<UserAccount>> GetUnsendedUsersAsync(int botId);
        Task UpdateUserAccountAsync(UserAccount user);
    }

    public class UserAccountRepository : IUserAccountRepository
    {
        private readonly ApplicationDbContext _context;

        public UserAccountRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddUserAccountAsync(UserAccount userAccount)
        {
            await _context.UserAccounts.AddAsync(userAccount);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UserExistsAsync(string blogName)
        {
            return await _context.UserAccounts.AnyAsync(u => u.BlogName == blogName);
        }

        public async Task<List<UserAccount>> GetAccountsByBotIdAsync(int botId, int pageNumber, int pageSize)
        {
            return await _context.UserAccounts
                .Where(account => account.BotId == botId)
                .OrderBy(account => account.CollectedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        public async Task<int> GetTotalAccountsCountAsync(int botId)
        {
            return await _context.UserAccounts
                .Where(account => account.BotId == botId)
                .CountAsync();
        }

        public async Task UpdateUserAccountAsync(UserAccount user)
        {
            _context.UserAccounts.Update(user);
            await _context.SaveChangesAsync();
        }
 

        public async Task<List<UserAccount>> GetUnsendedUsersAsync(int botId)
        {
            return await _context.UserAccounts
                .Where(u => u.BotId == botId && !u.IsQuestionSent)
                .ToListAsync();
        }
    }
}
