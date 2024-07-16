using DC.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DC.Services
{
  public class ApplicationDbContext : DbContext
  {
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {

    }
    public DbSet<UserAccount> UserAccounts { get; set; }
  }
}