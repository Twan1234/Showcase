using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Showcase.Infra.Interfaces;

namespace Showcase.Infra;

public class ShowcaseStorageInitializer : IHostedService
{
    private readonly ILogger<ShowcaseStorageInitializer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ShowcaseStorageInitializer(ILogger<ShowcaseStorageInitializer> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IGameSessionRepository>().Initialize();
        await scope.ServiceProvider.GetRequiredService<IUserConnectionRepository>().Initialize();
        await scope.ServiceProvider.GetRequiredService<IMoveRepository>().Initialize();
        await scope.ServiceProvider.GetRequiredService<IHighScoreRepository>().Initialize();

        _logger.LogInformation("Initialized TicTacToe storage (GameSessions, UserConnections, Moves, Highscores)");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
