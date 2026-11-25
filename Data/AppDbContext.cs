using Microsoft.EntityFrameworkCore;
using AquillaTextiles.Models;

namespace AquillaTextiles.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.NormalizedUsername)
            .IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.NormalizedEmail)
            .IsUnique();
        modelBuilder.Entity<User>()
            .Property(u => u.Username).HasMaxLength(40);
        modelBuilder.Entity<User>()
            .Property(u => u.NormalizedUsername).HasMaxLength(40);
        modelBuilder.Entity<User>()
            .Property(u => u.Email).HasMaxLength(254);
        modelBuilder.Entity<User>()
            .Property(u => u.NormalizedEmail).HasMaxLength(254);
    }
}
