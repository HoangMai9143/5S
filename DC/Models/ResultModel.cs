using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DC.Models
{
  [Table("Result")]
  public record ResultModel
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("survey_id")]
    public int SurveyId { get; set; }

    [Column("staff_id")]
    public int StaffId { get; set; }

    [Column("question_id")]
    public int QuestionId { get; set; }

    [Column("score")]
    public double Score { get; set; } // Store score in the scale of 100

    [Column("note")]
    public string? Note { get; set; }

    [ForeignKey("SurveyId")]
    public SurveyModel? Survey { get; set; }

    [ForeignKey("StaffId")]
    public StaffModel? Staff { get; set; }

    [ForeignKey("QuestionId")]
    public QuestionModel? Question { get; set; }
  }
}