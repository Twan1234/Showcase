using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Showcase.Controllers;
using Showcase.DataService;
using Showcase.Models;
using Xunit;

namespace Showcase.Test;

public class HighScoreControllerTests
{
    private readonly Mock<ILogger<HighScoreController>> _logger;
    private readonly Mock<ITicTacToeDbService> _dbService;
    private readonly HighScoreController _sut;

    public HighScoreControllerTests()
    {
        _logger = new Mock<ILogger<HighScoreController>>();
        _dbService = new Mock<ITicTacToeDbService>();
        _sut = new HighScoreController(_logger.Object, _dbService.Object);
    }

    [Fact]
    public async Task Index_ReturnsViewWithScoresOrderedByWinsByDefault()
    {
        var scores = new List<HighScore>
        {
            new() { Id = 1, PlayerName = "Alice", Wins = 5 },
            new() { Id = 2, PlayerName = "Bob", Wins = 3 }
        };
        _dbService.Setup(x => x.GetAllHighScoresAsync()).ReturnsAsync(scores);

        var result = await _sut.Index(sortOrder: null);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<HighScore>>(viewResult.Model);
        Assert.Equal(2, model.Count);
        Assert.Equal("Alice", model[0].PlayerName);
    }

    [Fact]
    public async Task Index_WithSortOrderPlayerName_OrdersByPlayerName()
    {
        var scores = new List<HighScore>
        {
            new() { Id = 1, PlayerName = "Zara", Wins = 1 },
            new() { Id = 2, PlayerName = "Alice", Wins = 10 }
        };
        _dbService.Setup(x => x.GetAllHighScoresAsync()).ReturnsAsync(scores);

        var result = await _sut.Index(sortOrder: "PlayerName");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<HighScore>>(viewResult.Model);
        Assert.Equal("Alice", model[0].PlayerName);
        Assert.Equal("Zara", model[1].PlayerName);
    }

    [Fact]
    public async Task Edit_WhenScoreNotFound_ReturnsNotFound()
    {
        _dbService.Setup(x => x.GetHighScoreByIdAsync(999)).ReturnsAsync((HighScore?)null);

        var result = await _sut.Edit(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_WhenScoreExists_ReturnsViewWithScore()
    {
        var score = new HighScore { Id = 1, PlayerName = "Test", Wins = 5 };
        _dbService.Setup(x => x.GetHighScoreByIdAsync(1)).ReturnsAsync(score);

        var result = await _sut.Edit(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(score, viewResult.Model);
    }

    [Fact]
    public async Task Delete_WhenScoreNotFound_ReturnsNotFound()
    {
        _dbService.Setup(x => x.GetHighScoreByIdAsync(999)).ReturnsAsync((HighScore?)null);

        var result = await _sut.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_CallsDeleteAndRedirectsToIndex()
    {
        var result = await _sut.DeleteConfirmed(1);

        _dbService.Verify(x => x.DeleteHighScoreAsync(1), Times.Once);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }
}
