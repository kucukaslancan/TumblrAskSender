using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace TumbAsk.Models
{
    public class Bot
    {
        private string _keyword;


        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Keyword
        {
            get => _keyword;
            set => _keyword = value?.Replace(' ', '+');
        }
        public int ThreadCount { get; set; }
        public int MaxAccounts { get; set; }
        public int MaxMessages { get; set; }
        public BotStatus Status { get; set; }
        public ICollection<BotLog> Logs { get; set; }

        public string RecurringJobId => $"BotJob-{Id}";  

    }
}
