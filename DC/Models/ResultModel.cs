using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DC.Models
{
  [Table("ResultModel")]
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

    [Column("grade")]
    public int Grade { get; set; }

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