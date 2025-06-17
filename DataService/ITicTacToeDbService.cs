using Showcase.Models;

namespace Showcase.DataService
{
    public interface ITicTacToeDbService
    {
        Task<GameSession> CreateOrJoinGameSessionAsync(string roomCode, string username, string connectionId);
        Task<bool> CheckOfGameVolIs(string roomCode);
        Task AddMoveAsync(string connectionId, int index, string symbol);
        Task<UserConnection?> GetConnectionByIdAsync(string connectionId);
        Task<GameSession?> GetSessionByRoomCodeAsync(string roomCode);
        Task RemoveConnectionAsync(string connectionId);
        Task AddHighScoresAsync(string connectionId, string PlayerSymbolWon);
        Task<List<HighScore>> GetAllHighScoresAsync();
        Task<HighScore?> GetHighScoreByIdAsync(int id);
        Task UpdateHighScoreAsync(HighScore highScore);
        Task DeleteHighScoreAsync(int id);
        Task SaveChangesAsync();
    }

}
