namespace TumbAsk.Models
{
    public class BotSettings
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Keyword { get; set; }
        public int ThreadCount { get; set; }
        public int MaxAccounts { get; set; }
        public int MaxMessages { get; set; }
    }
}
