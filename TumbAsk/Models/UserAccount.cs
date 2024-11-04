namespace TumbAsk.Models
{
    public class UserAccount
    {
        public int Id { get; set; }
        public string BlogName { get; set; }
        public DateTime CollectedAt { get; set; }
        public int BotId { get; set; }
        public bool IsQuestionSent { get; set; }
    }
}
