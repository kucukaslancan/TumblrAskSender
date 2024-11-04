namespace TumbAsk.Models
{
    public class BotLog
    {
        public int Id { get; set; }
        public int BotId { get; set; }
        public Bot Bot { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsSuccess { get; set; }
    }
}
