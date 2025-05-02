using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showcase.Models
{
    public class Move
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public int GameSessionId { get; set; }

        [ForeignKey("GameSessionId")]
        public GameSession GameSession { get; set; }
    }
}
