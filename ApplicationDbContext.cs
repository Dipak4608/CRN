using Microsoft.EntityFrameworkCore;
using SecureWebApi.Models;

namespace SecureWebApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasIndex(x => x.Name);

        modelBuilder.Entity<Product>()
            .HasIndex(x => x.Category);

        modelBuilder.Entity<Product>()
            .HasIndex(x => new
            {
                x.Category,
                x.Name
            });
        modelBuilder.Entity<User>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
    .HasIndex(x => x.TokenHash)
    .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}