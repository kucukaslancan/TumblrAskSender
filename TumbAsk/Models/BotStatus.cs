namespace TumbAsk.Models
{
    public enum BotStatus
    {
        Idle,
        Running,
        Completed,
        Error,
        Stopped, // Durdurulmuş botlar
        Paused   // Duraklatılmış botlar
    }
}
