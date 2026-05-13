using EquipTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipTrack.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Asset> Assets { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Allocation> Allocations { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Allocation>()
            .HasOne(a => a.Asset)
            .WithMany()
            .HasForeignKey(a => a.AssetId);

        modelBuilder.Entity<Allocation>()
            .HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).IsRequired();
            entity.HasIndex(u => u.Username).IsUnique();
        });
    }
}