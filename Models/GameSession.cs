namespace Showcase.Models
{
    public class GameSession
    {
        public int Id { get; set; }

        public string RoomCode { get; set; } = string.Empty;

        public ICollection<UserConnection> Players { get; set; } = new List<UserConnection>();
        public ICollection<Move> Moves { get; set; } = new List<Move>();

        public string CurrentTurnConnectionId { get; set; } = string.Empty;

        public bool IsGameOver { get; set; } = false;
    }
}
