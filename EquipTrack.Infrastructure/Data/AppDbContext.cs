using EquipTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipTrack.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Asset> Assets { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Allocation> Allocations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Explicitly map relationships if needed
        modelBuilder.Entity<Allocation>()
            .HasOne(a => a.Asset)
            .WithMany()
            .HasForeignKey(a => a.AssetId);

        modelBuilder.Entity<Allocation>()
            .HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId);
    }
}