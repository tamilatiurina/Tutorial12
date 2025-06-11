using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tutorial11.Models;

namespace Tutorial11.DAL;

public class DeviceContext : DbContext
{
    public DeviceContext(DbContextOptions<DeviceContext> options) : base(options)
    {
    }
    
    public DbSet<Person> Person { get; set; }
    public DbSet<Device> Device { get; set; }
    public DbSet<Account> Account { get; set; }
    public DbSet<DeviceEmployee> DeviceEmployee { get; set; }
    public DbSet<DeviceType> DeviceType { get; set; }
    public DbSet<Employee> Employee { get; set; }
    public DbSet<Position> Position { get; set; }
    public DbSet<Role> Role { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>()
            .HasIndex(d => d.PassportNumber)
            .IsUnique();
        
        modelBuilder.Entity<Device>(entity =>
        {
            entity.Property(e => e.AdditionalProperties)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null))
                .HasColumnType("nvarchar(max)");
        });
    }
}