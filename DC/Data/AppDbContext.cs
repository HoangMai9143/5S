using DC.Models;
using Microsoft.EntityFrameworkCore;

namespace DC.Data
{
  public class AppDbContext : DbContext
  {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }
    public DbSet<Question> Question { get; set; }
    public DbSet<Result> Result { get; set; }
    public DbSet<Survey> Survey { get; set; }
    public DbSet<SurveyQuestion> SurveyQuestion { get; set; }
    public DbSet<SurveyResult> SurveyResults { get; set; }
    public DbSet<User> User { get; set; }

  }
}