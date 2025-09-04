using Microsoft.EntityFrameworkCore;
using ProductOrderAPI.Domain.Entities;
using BCrypt.Net;
using ProductOrderAPI.Infrastructure.Security;

namespace ProductOrderAPI.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<User> Users { get; set; } 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .Property(p => p.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            modelBuilder.Entity<AuditLog>().ToTable("AuditLogs");
            

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = Guid.Parse("d7948ac8-a1c2-4adf-be5f-2b3ec7976139"),
                    Username = "admin",
                    PasswordHash = "$2a$11$09kqxVNLmgHxNAXJgg84weALfO2n.Hglevlz7Ddr..3CwtSoCzJWm",
                    Role = "Admin",
                    CreatedDate = new DateTime(2025, 9, 2)
                },
                new User
                {
                    Id = Guid.Parse("6f5c1ca5-5f78-4d15-b573-1c2109054fcf"),
                    Username = "user",
                    PasswordHash = "$2a$11$Qz3LfrWfEHF5r6NhaaTkS.csRaL2KeAI2RYmkebUaaNG49bYgn.RW",
                    Role = "User",
                    CreatedDate = new DateTime(2025, 9, 2)
                }
            );
        }
    }
}
