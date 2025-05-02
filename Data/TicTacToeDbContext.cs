using Microsoft.EntityFrameworkCore;
using Showcase.Models;
using System;

namespace Showcase.Data
{
    public class TicTacToeDbContext : DbContext
    {

        public TicTacToeDbContext(DbContextOptions<TicTacToeDbContext> options) : base(options) { }

        public DbSet<HighScore> Highscores { get; set; }
        public DbSet<GameSession> GameSessions { get; set; }
        public DbSet<UserConnection> UserConnections { get; set; }
        public DbSet<Move> Moves { get; set; }
    }
}
