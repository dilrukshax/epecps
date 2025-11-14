using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Epecps.Infrastructure.Persistence;

/// <summary>
/// Application database context
/// </summary>
public class EpecpsDbContext : DbContext
{
    public EpecpsDbContext(DbContextOptions<EpecpsDbContext> options) : base(options)
    {
    }

    #region Scoring Module DbSets
    public DbSet<ScoreTemplate> ScoreTemplates => Set<ScoreTemplate>();
    public DbSet<ScoreCategory> ScoreCategories => Set<ScoreCategory>();
    public DbSet<ScoreItem> ScoreItems => Set<ScoreItem>();
    #endregion

    #region Existing DbSets (placeholder for existing entities)
    // Add other DbSets here as needed
    // public DbSet<User> Users => Set<User>();
    // public DbSet<Department> Departments => Set<Department>();
    // etc.
    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
