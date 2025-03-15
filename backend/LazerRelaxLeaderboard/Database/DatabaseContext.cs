using LazerRelaxLeaderboard.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace LazerRelaxLeaderboard.Database;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

    private DatabaseContext() { }
    public DbSet<User> Users { get; set; } = null!;

    public DbSet<Score> Scores { get; set; } = null!;

    public DbSet<Beatmap> Beatmaps { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.TotalPp);
        modelBuilder.Entity<Score>().HasIndex(x => x.Pp);

        base.OnModelCreating(modelBuilder);
    }
}
