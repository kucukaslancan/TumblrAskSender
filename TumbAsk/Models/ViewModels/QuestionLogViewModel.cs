namespace TumbAsk.Models.ViewModels
{
    public class QuestionLogViewModel
    {
        public string Username { get; set; }
        public string Message { get; set; }
        public bool Status { get; set; } = false;
        public DateTime Timestamp { get; set; }
    }
}
