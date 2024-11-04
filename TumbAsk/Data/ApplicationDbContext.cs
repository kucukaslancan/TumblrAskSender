using Microsoft.EntityFrameworkCore;
using TumbAsk.Models;

namespace TumbAsk.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Bot> Bots { get; set; }
        public DbSet<BotLog> BotLogs { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }
    }
}
