using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showcase.Models
{
    public class UserConnection
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PlayerSymbol { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public int GameSessionId { get; set; }

        [ForeignKey("GameSessionId")]
        public GameSession GameSession { get; set; }

    }
}
