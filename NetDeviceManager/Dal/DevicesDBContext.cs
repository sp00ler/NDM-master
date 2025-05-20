using Microsoft.EntityFrameworkCore;
using NetDeviceManager.Models;

namespace NetDeviceManager.Dal
{
    public class DevicesDBContext : DbContext
    {
        public DevicesDBContext(DbContextOptions<DevicesDBContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<Device> Devices { get; set; }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(
                new User[]
                {
                new User {Id=1,  Login="User", Password=BCrypt.Net.BCrypt.HashPassword("Gfhjkm")

                }
                });
        }
    }
}
