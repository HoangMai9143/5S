using DC.Models;
using Microsoft.EntityFrameworkCore;

namespace DC.Data
{
  public class AppDbContext : DbContext
  {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }
    public DbSet<UserAccount> UserAccounts { get; set; }
  }
}