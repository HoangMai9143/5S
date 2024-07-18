using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DC.Models
{
  [Table("SurveyQuestionModel")]
  public record SurveyQuestionModel
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("survey_id")]
    public int SurveyId { get; set; }

    [Column("question_id")]
    public int QuestionId { get; set; }

    [ForeignKey("SurveyId")]
    public SurveyModel? Survey { get; set; }

    [ForeignKey("QuestionId")]
    public QuestionModel? Question { get; set; }
  }
}