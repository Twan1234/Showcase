using Showcase.Models;
using Xunit;

namespace Showcase.Test;

public class HighScoreModelTests
{
    [Fact]
    public void HighScore_DefaultValues_AreSet()
    {
        var score = new HighScore();

        Assert.Equal(0, score.Id);
        Assert.Equal(string.Empty, score.PlayerName);
        Assert.Equal(0, score.Wins);
        Assert.Equal(0, score.Losses);
        Assert.Equal(0, score.Draws);
        Assert.True((DateTime.UtcNow - score.LastPlayed).TotalSeconds < 2);
    }

    [Fact]
    public void HighScore_Properties_CanBeSet()
    {
        var lastPlayed = DateTime.UtcNow.AddDays(-1);
        var score = new HighScore
        {
            Id = 42,
            PlayerName = "Player1",
            Wins = 10,
            Losses = 3,
            Draws = 2,
            LastPlayed = lastPlayed
        };

        Assert.Equal(42, score.Id);
        Assert.Equal("Player1", score.PlayerName);
        Assert.Equal(10, score.Wins);
        Assert.Equal(3, score.Losses);
        Assert.Equal(2, score.Draws);
        Assert.Equal(lastPlayed, score.LastPlayed);
    }
}
