using DC.Models;
using Microsoft.EntityFrameworkCore;

namespace DC.Services
{
  public class AppDbContext : DbContext
  {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }
    public DbSet<UserAccount> UserAccounts { get; set; }
  }
}