using Showcase.Models;

namespace Showcase.Infra.Interfaces;

public interface IHighScoreRepository : IRepositoryStore
{
    Task<HighScore?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<HighScore?> GetByPlayerNameAsync(string playerName, CancellationToken ct = default);
    Task<IReadOnlyList<HighScore>> GetAllOrderByWinsDescAsync(CancellationToken ct = default);
    Task<HighScore> CreateAsync(HighScore highScore, CancellationToken ct = default);
    Task UpdateAsync(HighScore highScore, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
