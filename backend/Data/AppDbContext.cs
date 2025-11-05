using backend.Helpers;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<ImageRef> Images { get; set; }
    public DbSet<TileCollection> TileCollections { get; set; }
    
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var currentTimestamp = DatabaseHelper.IsSqlite(Database) ? "CURRENT_TIMESTAMP" : "NOW()";
        
        modelBuilder.Entity<User>()
            .Property(u => u.CreatedAt)
            .HasDefaultValueSql(currentTimestamp)
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<Project>()
            .Property(p => p.CreatedAt)
            .HasDefaultValueSql(currentTimestamp)
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<Job>()
            .Property(t => t.StartedAt)
            .HasDefaultValueSql(currentTimestamp)
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<RefreshToken>()
            .Property(u => u.CreatedAt)
            .HasDefaultValueSql(currentTimestamp)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<Job>()
            .HasOne(j => j.TargetImage)
            .WithMany()
            .HasForeignKey(j => j.TargetImageId);
    }
}