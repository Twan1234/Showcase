using Showcase.Data;
using Showcase.Models;
using Microsoft.EntityFrameworkCore;

namespace Showcase.DataService
{
    public class TicTacToeDbService : ITicTacToeDbService
    {
        private readonly TicTacToeDbContext _context;

        public TicTacToeDbService(TicTacToeDbContext context)
        {
            _context = context;
        }
        public async Task<GameSession> CreateOrJoinGameSessionAsync(string roomCode, string username, string connectionId)
        {
            var session = await _context.GameSessions
                .Include(s => s.Players)
                .FirstOrDefaultAsync(s => s.RoomCode == roomCode);

            if (session == null)
            {
                session = new GameSession
                {
                    RoomCode = roomCode,
                    CurrentTurnConnectionId = connectionId
                };

                _context.GameSessions.Add(session);
                await _context.SaveChangesAsync();
            }

            if (!session.Players.Any(p => p.ConnectionId == connectionId))
            {
                string symbol = session.Players.Count == 0 ? "x" : "o";

                var user = new UserConnection
                {
                    Username = username,
                    ConnectionId = connectionId,
                    PlayerSymbol = symbol,
                    GameSessionId = session.Id
                };


                _context.UserConnections.Add(user);
                await _context.SaveChangesAsync();

                Console.WriteLine($"New user created with Id: {user.Id}");
            }

            return session;
        }

        public async Task<bool> CheckOfGameVolIs(string roomCode)
        {
            var session = await _context.GameSessions
                .Include(s => s.Players)
                .FirstOrDefaultAsync(s => s.RoomCode == roomCode);

            if (session != null && session.Players.Count >= 2)
            {
                return true;
            }

            return false;
        }

        public async Task AddMoveAsync(string connectionId, int index, string symbol)
        {
            var conn = await GetConnectionByIdAsync(connectionId);
            if (conn == null) return;

            var move = new Move
            {
                GameSessionId = conn.GameSessionId,
                Index = index,
                Symbol = symbol
            };

            _context.Moves.Add(move);
            await _context.SaveChangesAsync();
        }

        public async Task<UserConnection?> GetConnectionByIdAsync(string connectionId)
        {
            return await _context.UserConnections
                .Include(c => c.GameSession)
                .ThenInclude(gs => gs.Players)
                .Include(c => c.GameSession)
                .ThenInclude(gs => gs.Moves)
                .FirstOrDefaultAsync(c => c.ConnectionId == connectionId);
        }
        public async Task<GameSession?> GetSessionByRoomCodeAsync(string roomCode)
        {
            return await _context.GameSessions
                .Include(s => s.Players)
                .Include(s => s.Moves)
                .FirstOrDefaultAsync(s => s.RoomCode == roomCode);
        }

        public async Task RemoveConnectionAsync(string connectionId)
        {
            var conn = await _context.UserConnections
                .FirstOrDefaultAsync(c => c.ConnectionId == connectionId);

            if (conn != null)
            {
                _context.UserConnections.Remove(conn);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<HighScore?> GetHighScoreByIdAsync(int id)
        {
            return await _context.Highscores.FindAsync(id);
        }

        public async Task<HighScore?> GetHighScoreByPlayerNameAsync(string playerName)
        {

           return await _context.Highscores
                    .FirstOrDefaultAsync(c => c.PlayerName == playerName);
        }

        public async Task AddHighScoresAsync(string connectionId, string PlayerSymbolWon)
        {
            var conn = await GetConnectionByIdAsync(connectionId);
            if (conn == null) return;

            foreach (var item in conn.GameSession.Players)
            {
                var highScore = await GetHighScoreByPlayerNameAsync(item.Username);

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
                }

                if (PlayerSymbolWon == "DRAW")
                {
                    highScore.Draws += 1;
                    highScore.LastPlayed = DateTime.UtcNow;

                } else if (PlayerSymbolWon == item.PlayerSymbol)
                {
                    highScore.Wins += 1;
                    highScore.LastPlayed = DateTime.UtcNow;
                }
                else
                {
                    highScore.Losses += 1;
                    highScore.LastPlayed = DateTime.UtcNow;
                }

                if (highScore.Id == 0)
                {
                    _context.Highscores.Add(highScore);
                }
                else
                {
                    _context.Highscores.Update(highScore);
                }
            }


          
            await _context.SaveChangesAsync();





        }

        public async Task<List<HighScore>> GetAllHighScoresAsync()
        {
            return await _context.Highscores
                .OrderByDescending(h => h.Wins)
                .ThenBy(h => h.PlayerName)
                .ToListAsync();
        }

        public async Task UpdateHighScoreAsync(HighScore highScore)
        {
            _context.Highscores.Update(highScore);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteHighScoreAsync(int id)
        {
            var highScore = await _context.Highscores.FindAsync(id);
            if (highScore != null)
            {
                _context.Highscores.Remove(highScore);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }


    }
}
