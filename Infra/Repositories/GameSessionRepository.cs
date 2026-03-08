using SqlKata.Compilers;
using SqlKata.Execution;
using Showcase.Infra.Interfaces;
using Showcase.Models;

namespace Showcase.Infra.Repositories;

public class GameSessionRepository : IGameSessionRepository
{
    private readonly QueryFactory _db;
    private const string TableName = "GameSessions";

    public GameSessionRepository(QueryFactory db) => _db = db;

    public async Task Initialize()
    {
        switch (_db.Compiler)
        {
            case SqliteCompiler:
                await _db.StatementAsync($@"
                    CREATE TABLE IF NOT EXISTS {TableName}
                    (
                        Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        RoomCode TEXT NOT NULL,
                        CurrentTurnConnectionId TEXT NOT NULL,
                        IsGameOver INTEGER NOT NULL DEFAULT 0
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS IX_{TableName}_RoomCode ON {TableName}(RoomCode);
                ");
                break;
            case SqlServerCompiler:
                await _db.StatementAsync($@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.{TableName}') AND type = N'U')
                    BEGIN
                        CREATE TABLE dbo.{TableName}
                        (
                            Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
                            RoomCode NVARCHAR(50) NOT NULL,
                            CurrentTurnConnectionId NVARCHAR(256) NOT NULL,
                            IsGameOver BIT NOT NULL CONSTRAINT DF_{TableName}_IsGameOver DEFAULT 0
                        );
                        CREATE UNIQUE INDEX IX_{TableName}_RoomCode ON dbo.{TableName}(RoomCode);
                    END
                ");
                break;
            default:
                throw new NotSupportedException("Only SQLite and SQL Server are supported.");
        }
    }

    public async Task<GameSession?> GetByRoomCodeAsync(string roomCode, CancellationToken ct = default) =>
        await _db.Query(TableName).Where("RoomCode", roomCode).FirstOrDefaultAsync<GameSession>(cancellationToken: ct);

    public async Task<GameSession?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _db.Query(TableName).Where("Id", id).FirstOrDefaultAsync<GameSession>(cancellationToken: ct);

    public async Task<GameSession> CreateAsync(string roomCode, string currentTurnConnectionId, CancellationToken ct = default)
    {
        await _db.Query(TableName).InsertAsync(new
        {
            RoomCode = roomCode,
            CurrentTurnConnectionId = currentTurnConnectionId,
            IsGameOver = false
        }, cancellationToken: ct);
        var created = await GetByRoomCodeAsync(roomCode, ct) ?? throw new InvalidOperationException("Insert succeeded but row not found.");
        return created;
    }

    public async Task UpdateAsync(int id, string? currentTurnConnectionId = null, bool? isGameOver = null, CancellationToken ct = default)
    {
        if (currentTurnConnectionId != null && isGameOver.HasValue)
            await _db.Query(TableName).Where("Id", id).UpdateAsync(new { CurrentTurnConnectionId = currentTurnConnectionId, IsGameOver = isGameOver.Value }, cancellationToken: ct);
        else if (currentTurnConnectionId != null)
            await _db.Query(TableName).Where("Id", id).UpdateAsync(new { CurrentTurnConnectionId = currentTurnConnectionId }, cancellationToken: ct);
        else if (isGameOver.HasValue)
            await _db.Query(TableName).Where("Id", id).UpdateAsync(new { IsGameOver = isGameOver.Value }, cancellationToken: ct);
    }
}
