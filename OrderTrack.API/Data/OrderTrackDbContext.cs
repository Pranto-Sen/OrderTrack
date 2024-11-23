using Microsoft.EntityFrameworkCore;
using OrderTrack.API.Models;

namespace OrderTrack.API.Data
{
    public class OrderTrackDbContext:DbContext
    {
        public OrderTrackDbContext(DbContextOptions dbContextOptions): base(dbContextOptions) 
        {
            
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
