using SqlKata.Compilers;
using SqlKata.Execution;
using Showcase.Infra.Interfaces;
using Showcase.Models;

namespace Showcase.Infra.Repositories;

public class MoveRepository : IMoveRepository
{
    private readonly QueryFactory _db;
    private const string TableName = "Moves";

    public MoveRepository(QueryFactory db) => _db = db;

    public async Task Initialize()
    {
        switch (_db.Compiler)
        {
            case SqliteCompiler:
                await _db.StatementAsync($@"
                    CREATE TABLE IF NOT EXISTS {TableName}
                    (
                        Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        [Index] INTEGER NOT NULL,
                        Symbol TEXT NOT NULL,
                        GameSessionId INTEGER NOT NULL,
                        FOREIGN KEY (GameSessionId) REFERENCES GameSessions(Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS IX_{TableName}_GameSessionId ON {TableName}(GameSessionId);
                ");
                break;
            case SqlServerCompiler:
                await _db.StatementAsync($@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.{TableName}') AND type = N'U')
                    BEGIN
                        CREATE TABLE dbo.{TableName}
                        (
                            Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
                            [Index] INT NOT NULL,
                            Symbol NVARCHAR(10) NOT NULL,
                            GameSessionId INT NOT NULL,
                            CONSTRAINT FK_{TableName}_GameSessions FOREIGN KEY (GameSessionId) REFERENCES dbo.GameSessions(Id) ON DELETE CASCADE
                        );
                        CREATE INDEX IX_{TableName}_GameSessionId ON dbo.{TableName}(GameSessionId);
                    END
                ");
                break;
            default:
                throw new NotSupportedException("Only SQLite and SQL Server are supported.");
        }
    }

    public async Task<IReadOnlyList<Move>> GetByGameSessionIdAsync(int gameSessionId, CancellationToken ct = default)
    {
        var list = await _db.Query(TableName).Where("GameSessionId", gameSessionId).GetAsync<Move>(cancellationToken: ct);
        return list?.ToList() ?? new List<Move>();
    }

    public async Task AddAsync(int gameSessionId, int index, string symbol, CancellationToken ct = default) =>
        await _db.Query(TableName).InsertAsync(new { GameSessionId = gameSessionId, Index = index, Symbol = symbol }, cancellationToken: ct);

    public async Task ClearForGameSessionAsync(int gameSessionId, CancellationToken ct = default) =>
        await _db.Query(TableName).Where("GameSessionId", gameSessionId).DeleteAsync(cancellationToken: ct);
}
