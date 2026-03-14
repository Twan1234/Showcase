using Moq;
using Showcase.DataService;
using Showcase.Infra.Interfaces;
using Showcase.Models;
using Xunit;

namespace Showcase.Test;

public class TicTacToeDbServiceTests
{
    private readonly Mock<IGameSessionRepository> _gameSessions;
    private readonly Mock<IUserConnectionRepository> _userConnections;
    private readonly Mock<IMoveRepository> _moves;
    private readonly Mock<IHighScoreRepository> _highScores;
    private readonly TicTacToeDbService _sut;

    public TicTacToeDbServiceTests()
    {
        _gameSessions = new Mock<IGameSessionRepository>();
        _userConnections = new Mock<IUserConnectionRepository>();
        _moves = new Mock<IMoveRepository>();
        _highScores = new Mock<IHighScoreRepository>();
        _sut = new TicTacToeDbService(
            _gameSessions.Object,
            _userConnections.Object,
            _moves.Object,
            _highScores.Object);
    }

    [Fact]
    public async Task CreateOrJoinGameSessionAsync_WhenNoSession_CreatesNewSessionAndAddsFirstPlayer()
    {
        var roomCode = "ROOM1";
        var username = "Alice";
        var connectionId = "conn-1";

        _gameSessions.Setup(x => x.GetByRoomCodeAsync(roomCode, default)).ReturnsAsync((GameSession?)null);
        var newSession = new GameSession { Id = 1, RoomCode = roomCode };
        _gameSessions.Setup(x => x.CreateAsync(roomCode, connectionId, default)).ReturnsAsync(newSession);
        _userConnections.Setup(x => x.GetByGameSessionIdAsync(1, default)).ReturnsAsync(Array.Empty<UserConnection>().ToList());
        var createdConn = new UserConnection { Id = 1, Username = username, PlayerSymbol = "x", ConnectionId = connectionId, GameSessionId = 1 };
        _userConnections.Setup(x => x.CreateAsync(username, "x", connectionId, 1, default)).ReturnsAsync(createdConn);
        _userConnections.Setup(x => x.GetByGameSessionIdAsync(1, default)).ReturnsAsync(new List<UserConnection> { createdConn });
        _moves.Setup(x => x.GetByGameSessionIdAsync(1, default)).ReturnsAsync(new List<Move>());

        var result = await _sut.CreateOrJoinGameSessionAsync(roomCode, username, connectionId);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Single(result.Players);
        Assert.Equal("x", result.Players.First().PlayerSymbol);
    }

    [Fact]
    public async Task CheckOfGameVolIs_WhenSessionNull_ReturnsFalse()
    {
        _gameSessions.Setup(x => x.GetByRoomCodeAsync("MISSING", default)).ReturnsAsync((GameSession?)null);

        var result = await _sut.CheckOfGameVolIs("MISSING");

        Assert.False(result);
    }

    [Fact]
    public async Task CheckOfGameVolIs_WhenTwoPlayers_ReturnsTrue()
    {
        var session = new GameSession { Id = 1 };
        _gameSessions.Setup(x => x.GetByRoomCodeAsync("ROOM1", default)).ReturnsAsync(session);
        _userConnections.Setup(x => x.GetByGameSessionIdAsync(1, default)).ReturnsAsync(new List<UserConnection>
        {
            new() { Id = 1 }, new() { Id = 2 }
        });

        var result = await _sut.CheckOfGameVolIs("ROOM1");

        Assert.True(result);
    }

    [Fact]
    public async Task GetHighScoreByIdAsync_DelegatesToRepository()
    {
        var score = new HighScore { Id = 1, PlayerName = "Test", Wins = 5 };
        _highScores.Setup(x => x.GetByIdAsync(1, default)).ReturnsAsync(score);

        var result = await _sut.GetHighScoreByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.PlayerName);
    }

    [Fact]
    public async Task GetAllHighScoresAsync_ReturnsOrderedList()
    {
        var list = new List<HighScore>
        {
            new() { Id = 1, PlayerName = "A", Wins = 10 },
            new() { Id = 2, PlayerName = "B", Wins = 5 }
        };
        _highScores.Setup(x => x.GetAllOrderByWinsDescAsync(default)).ReturnsAsync(list);

        var result = await _sut.GetAllHighScoresAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(10, result[0].Wins);
    }
}
