using Showcase.Models;

namespace Showcase.Infra.Interfaces;

public interface IUserConnectionRepository : IRepositoryStore
{
    Task<UserConnection?> GetByConnectionIdAsync(string connectionId, CancellationToken ct = default);
    Task<IReadOnlyList<UserConnection>> GetByGameSessionIdAsync(int gameSessionId, CancellationToken ct = default);
    Task<UserConnection> CreateAsync(string username, string playerSymbol, string connectionId, int gameSessionId, CancellationToken ct = default);
    Task RemoveByConnectionIdAsync(string connectionId, CancellationToken ct = default);
}
