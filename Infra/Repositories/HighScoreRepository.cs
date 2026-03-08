using SqlKata.Compilers;
using SqlKata.Execution;
using Showcase.Infra.Interfaces;
using Showcase.Models;

namespace Showcase.Infra.Repositories;

public class HighScoreRepository : IHighScoreRepository
{
    private readonly QueryFactory _db;
    private const string TableName = "Highscores";

    public HighScoreRepository(QueryFactory db) => _db = db;

    public async Task Initialize()
    {
        switch (_db.Compiler)
        {
            case SqliteCompiler:
                await _db.StatementAsync($@"
                    CREATE TABLE IF NOT EXISTS {TableName}
                    (
                        Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        PlayerName TEXT NOT NULL,
                        Wins INTEGER NOT NULL DEFAULT 0,
                        Losses INTEGER NOT NULL DEFAULT 0,
                        Draws INTEGER NOT NULL DEFAULT 0,
                        LastPlayed TEXT NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS IX_{TableName}_PlayerName ON {TableName}(PlayerName);
                ");
                break;
            case SqlServerCompiler:
                await _db.StatementAsync($@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.{TableName}') AND type = N'U')
                    BEGIN
                        CREATE TABLE dbo.{TableName}
                        (
                            Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
                            PlayerName NVARCHAR(256) NOT NULL,
                            Wins INT NOT NULL CONSTRAINT DF_{TableName}_Wins DEFAULT 0,
                            Losses INT NOT NULL CONSTRAINT DF_{TableName}_Losses DEFAULT 0,
                            Draws INT NOT NULL CONSTRAINT DF_{TableName}_Draws DEFAULT 0,
                            LastPlayed DATETIME2 NOT NULL
                        );
                        CREATE UNIQUE INDEX IX_{TableName}_PlayerName ON dbo.{TableName}(PlayerName);
                    END
                ");
                break;
            default:
                throw new NotSupportedException("Only SQLite and SQL Server are supported.");
        }
    }

    public async Task<HighScore?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _db.Query(TableName).Where("Id", id).FirstOrDefaultAsync<HighScore>(cancellationToken: ct);

    public async Task<HighScore?> GetByPlayerNameAsync(string playerName, CancellationToken ct = default) =>
        await _db.Query(TableName).Where("PlayerName", playerName).FirstOrDefaultAsync<HighScore>(cancellationToken: ct);

    public async Task<IReadOnlyList<HighScore>> GetAllOrderByWinsDescAsync(CancellationToken ct = default)
    {
        var list = await _db.Query(TableName)
            .OrderByDesc("Wins")
            .OrderBy("PlayerName")
            .GetAsync<HighScore>(cancellationToken: ct);
        return list?.ToList() ?? new List<HighScore>();
    }

    public async Task<HighScore> CreateAsync(HighScore highScore, CancellationToken ct = default)
    {
        await _db.Query(TableName).InsertAsync(new
        {
            highScore.PlayerName,
            highScore.Wins,
            highScore.Losses,
            highScore.Draws,
            highScore.LastPlayed
        }, cancellationToken: ct);
        var created = await GetByPlayerNameAsync(highScore.PlayerName, ct) ?? throw new InvalidOperationException("Insert succeeded but row not found.");
        return created;
    }

    public async Task UpdateAsync(HighScore highScore, CancellationToken ct = default) =>
        await _db.Query(TableName).Where("Id", highScore.Id).UpdateAsync(new
        {
            highScore.PlayerName,
            highScore.Wins,
            highScore.Losses,
            highScore.Draws,
            highScore.LastPlayed
        }, cancellationToken: ct);

    public async Task DeleteAsync(int id, CancellationToken ct = default) =>
        await _db.Query(TableName).Where("Id", id).DeleteAsync(cancellationToken: ct);
}
