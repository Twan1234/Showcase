using SqlKata.Compilers;
using SqlKata.Execution;
using Showcase.Infra.Interfaces;
using Showcase.Models;

namespace Showcase.Infra.Repositories;

public class UserConnectionRepository : IUserConnectionRepository
{
    private readonly QueryFactory _db;
    private const string TableName = "UserConnections";

    public UserConnectionRepository(QueryFactory db) => _db = db;

    public async Task Initialize()
    {
        switch (_db.Compiler)
        {
            case SqliteCompiler:
                await _db.StatementAsync($@"
                    CREATE TABLE IF NOT EXISTS {TableName}
                    (
                        Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL,
                        PlayerSymbol TEXT NOT NULL,
                        ConnectionId TEXT NOT NULL,
                        GameSessionId INTEGER NOT NULL,
                        FOREIGN KEY (GameSessionId) REFERENCES GameSessions(Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS IX_{TableName}_ConnectionId ON {TableName}(ConnectionId);
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
                            Username NVARCHAR(256) NOT NULL,
                            PlayerSymbol NVARCHAR(10) NOT NULL,
                            ConnectionId NVARCHAR(256) NOT NULL,
                            GameSessionId INT NOT NULL,
                            CONSTRAINT FK_{TableName}_GameSessions FOREIGN KEY (GameSessionId) REFERENCES dbo.GameSessions(Id) ON DELETE CASCADE
                        );
                        CREATE INDEX IX_{TableName}_ConnectionId ON dbo.{TableName}(ConnectionId);
                        CREATE INDEX IX_{TableName}_GameSessionId ON dbo.{TableName}(GameSessionId);
                    END
                ");
                break;
            default:
                throw new NotSupportedException("Only SQLite and SQL Server are supported.");
        }
    }

    public async Task<UserConnection?> GetByConnectionIdAsync(string connectionId, CancellationToken ct = default) =>
        await _db.Query(TableName).Where("ConnectionId", connectionId).FirstOrDefaultAsync<UserConnection>(cancellationToken: ct);

    public async Task<IReadOnlyList<UserConnection>> GetByGameSessionIdAsync(int gameSessionId, CancellationToken ct = default)
    {
        var list = await _db.Query(TableName).Where("GameSessionId", gameSessionId).GetAsync<UserConnection>(cancellationToken: ct);
        return list?.ToList() ?? new List<UserConnection>();
    }

    public async Task<UserConnection> CreateAsync(string username, string playerSymbol, string connectionId, int gameSessionId, CancellationToken ct = default)
    {
        await _db.Query(TableName).InsertAsync(new
        {
            Username = username,
            PlayerSymbol = playerSymbol,
            ConnectionId = connectionId,
            GameSessionId = gameSessionId
        }, cancellationToken: ct);
        var created = await GetByConnectionIdAsync(connectionId, ct) ?? throw new InvalidOperationException("Insert succeeded but row not found.");
        return created;
    }

    public async Task RemoveByConnectionIdAsync(string connectionId, CancellationToken ct = default) =>
        await _db.Query(TableName).Where("ConnectionId", connectionId).DeleteAsync(cancellationToken: ct);
}
