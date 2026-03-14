using Showcase.Models;

namespace Showcase.Infra.Interfaces;

public interface IMoveRepository : IRepositoryStore
{
    Task<IReadOnlyList<Move>> GetByGameSessionIdAsync(int gameSessionId, CancellationToken ct = default);
    Task AddAsync(int gameSessionId, int index, string symbol, CancellationToken ct = default);
    Task ClearForGameSessionAsync(int gameSessionId, CancellationToken ct = default);
}
