using Showcase.Models;

namespace Showcase.Infra.Interfaces;

public interface IGameSessionRepository : IRepositoryStore
{
    Task<GameSession?> GetByRoomCodeAsync(string roomCode, CancellationToken ct = default);
    Task<GameSession?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<GameSession> CreateAsync(string roomCode, string currentTurnConnectionId, CancellationToken ct = default);
    Task UpdateAsync(int id, string? currentTurnConnectionId = null, bool? isGameOver = null, CancellationToken ct = default);
}
