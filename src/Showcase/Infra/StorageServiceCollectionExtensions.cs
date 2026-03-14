using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Showcase.Areas.Identity.Data;
using Showcase.Data;
using Showcase.DataService;
using SqlKata.Compilers;
using SqlKata.Execution;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Showcase.Infra.Interfaces;
using Showcase.Infra.Repositories;

namespace Showcase.Infra;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddShowcaseStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dbProvider = configuration["DbProvider"] ?? "Sqlite";
        var useSqlite = dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);

        var authCs = configuration.GetConnectionString("AuthDbContextConnection")
            ?? throw new InvalidOperationException("Connection string 'AuthDbContextConnection' not found.");
        if (useSqlite)
            services.AddDbContext<AuthDbContext>(o => o.UseSqlite(authCs));
        else
            services.AddDbContext<AuthDbContext>(o => o.UseSqlServer(authCs));

        _ = configuration.GetConnectionString("TicTacToeDbConnection")
            ?? throw new InvalidOperationException("Connection string 'TicTacToeDbConnection' not found.");

        services.AddScoped<QueryFactory>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var useSqliteKata = (cfg["DbProvider"] ?? "Sqlite").Equals("Sqlite", StringComparison.OrdinalIgnoreCase);
            var connStr = cfg.GetConnectionString("TicTacToeDbConnection")!;

            if (useSqliteKata)
            {
                var conn = new SqliteConnection(connStr);
                return new QueryFactory(conn, new SqliteCompiler());
            }
            else
            {
                var conn = new SqlConnection(connStr);
                return new QueryFactory(conn, new SqlServerCompiler());
            }
        });

        services.AddScoped<IGameSessionRepository, GameSessionRepository>();
        services.AddScoped<IUserConnectionRepository, UserConnectionRepository>();
        services.AddScoped<IMoveRepository, MoveRepository>();
        services.AddScoped<IHighScoreRepository, HighScoreRepository>();
        services.AddScoped<ITicTacToeDbService, TicTacToeDbService>();
        services.AddHostedService<ShowcaseStorageInitializer>();

        return services;
    }

    public static bool IsUsingSqlite(IConfiguration configuration)
    {
        var dbProvider = configuration["DbProvider"] ?? "Sqlite";
        return dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);
    }
}
