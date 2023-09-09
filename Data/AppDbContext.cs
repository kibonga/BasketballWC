using BasketballWC.Models;
using Microsoft.EntityFrameworkCore;
namespace BasketballWC.Data;

public class AppDbContext : DbContext {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Models.Team> Teams {get; set;}
    public DbSet<Models.Player> Players {get; set;}
}