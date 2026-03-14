using Showcase.Infra.Interfaces;
using Showcase.Models;

namespace Showcase.DataService;

public class TicTacToeDbService : ITicTacToeDbService
{
    private readonly IGameSessionRepository _gameSessions;
    private readonly IUserConnectionRepository _userConnections;
    private readonly IMoveRepository _moves;
    private readonly IHighScoreRepository _highScores;

    public TicTacToeDbService(
        IGameSessionRepository gameSessions,
        IUserConnectionRepository userConnections,
        IMoveRepository moves,
        IHighScoreRepository highScores)
    {
        _gameSessions = gameSessions;
        _userConnections = userConnections;
        _moves = moves;
        _highScores = highScores;
    }

    public async Task<GameSession> CreateOrJoinGameSessionAsync(string roomCode, string username, string connectionId)
    {
        var session = await _gameSessions.GetByRoomCodeAsync(roomCode);
        if (session == null)
        {
            session = await _gameSessions.CreateAsync(roomCode, connectionId);
        }

        var players = await _userConnections.GetByGameSessionIdAsync(session.Id);
        if (players.All(p => p.ConnectionId != connectionId))
        {
            var symbol = players.Count == 0 ? "x" : "o";
            await _userConnections.CreateAsync(username, symbol, connectionId, session.Id);
            players = await _userConnections.GetByGameSessionIdAsync(session.Id);
        }

        session.Players = players.ToList();
        session.Moves = (await _moves.GetByGameSessionIdAsync(session.Id)).ToList();
        return session;
    }

    public async Task<bool> CheckOfGameVolIs(string roomCode)
    {
        var session = await _gameSessions.GetByRoomCodeAsync(roomCode);
        if (session == null) return false;
        var players = await _userConnections.GetByGameSessionIdAsync(session.Id);
        return players.Count >= 2;
    }

    public async Task AddMoveAsync(string connectionId, int index, string symbol)
    {
        var conn = await _userConnections.GetByConnectionIdAsync(connectionId);
        if (conn == null) return;
        await _moves.AddAsync(conn.GameSessionId, index, symbol);
    }

    public async Task<UserConnection?> GetConnectionByIdAsync(string connectionId)
    {
        var conn = await _userConnections.GetByConnectionIdAsync(connectionId);
        if (conn == null) return null;
        var session = await _gameSessions.GetByIdAsync(conn.GameSessionId);
        if (session == null) return conn;
        session.Players = (await _userConnections.GetByGameSessionIdAsync(session.Id)).ToList();
        session.Moves = (await _moves.GetByGameSessionIdAsync(session.Id)).ToList();
        conn.GameSession = session;
        return conn;
    }

    public async Task<GameSession?> GetSessionByRoomCodeAsync(string roomCode)
    {
        var session = await _gameSessions.GetByRoomCodeAsync(roomCode);
        if (session == null) return null;
        session.Players = (await _userConnections.GetByGameSessionIdAsync(session.Id)).ToList();
        session.Moves = (await _moves.GetByGameSessionIdAsync(session.Id)).ToList();
        return session;
    }

    public async Task RemoveConnectionAsync(string connectionId) =>
        await _userConnections.RemoveByConnectionIdAsync(connectionId);

    public async Task<HighScore?> GetHighScoreByIdAsync(int id) =>
        await _highScores.GetByIdAsync(id);

    public async Task<HighScore?> GetHighScoreByPlayerNameAsync(string playerName) =>
        await _highScores.GetByPlayerNameAsync(playerName);

    public async Task AddHighScoresAsync(string connectionId, string playerSymbolWon)
    {
        var conn = await GetConnectionByIdAsync(connectionId);
        if (conn?.GameSession == null) return;

        foreach (var item in conn.GameSession.Players)
        {
            var highScore = await _highScores.GetByPlayerNameAsync(item.Username);
            if (highScore == null)
            {
                highScore = new HighScore
                {
                    PlayerName = item.Username,
                    Wins = 0,
                    Losses = 0,
                    Draws = 0,
                    LastPlayed = DateTime.UtcNow
                };
                highScore = await _highScores.CreateAsync(highScore);
            }

            if (playerSymbolWon == "DRAW")
            {
                highScore.Draws += 1;
            }
            else if (playerSymbolWon == item.PlayerSymbol)
            {
                highScore.Wins += 1;
            }
            else
            {
                highScore.Losses += 1;
            }
            highScore.LastPlayed = DateTime.UtcNow;
            await _highScores.UpdateAsync(highScore);
        }
    }

    public async Task<List<HighScore>> GetAllHighScoresAsync()
    {
        var list = await _highScores.GetAllOrderByWinsDescAsync();
        return list.ToList();
    }

    public async Task UpdateHighScoreAsync(HighScore highScore) =>
        await _highScores.UpdateAsync(highScore);

    public async Task DeleteHighScoreAsync(int id) =>
        await _highScores.DeleteAsync(id);

    public Task SaveChangesAsync() => Task.CompletedTask;

    public async Task UpdateSessionTurnAsync(int sessionId, string currentTurnConnectionId) =>
        await _gameSessions.UpdateAsync(sessionId, currentTurnConnectionId: currentTurnConnectionId);

    public async Task ClearMovesAndUpdateTurnAsync(int sessionId, string currentTurnConnectionId)
    {
        await _moves.ClearForGameSessionAsync(sessionId);
        await _gameSessions.UpdateAsync(sessionId, currentTurnConnectionId: currentTurnConnectionId);
    }

    public async Task SetSessionGameOverAsync(int sessionId, bool isGameOver = true) =>
        await _gameSessions.UpdateAsync(sessionId, isGameOver: isGameOver);
}
