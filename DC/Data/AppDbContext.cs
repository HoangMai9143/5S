using DC.Models;
using Microsoft.EntityFrameworkCore;

namespace DC.Data
{
  public class AppDbContext : DbContext
  {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Models.QuestionModel> QuestionModel { get; set; }
    public DbSet<ResultModel> ResultModel { get; set; }
    public DbSet<StaffModel> StaffModel { get; set; }
    public DbSet<SurveyModel> SurveyModel { get; set; }
    public DbSet<SurveyQuestionModel> SurveyQuestionModel { get; set; }
    public DbSet<SurveyResultModel> SurveyResultModel { get; set; }
    public DbSet<UserAccountModel> UserModel { get; set; }

  }
}