namespace Showcase.Models
{
    public class HighScore
    {
        public int Id { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public DateTime LastPlayed { get; set; } = DateTime.UtcNow;
    }
}
