using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace DC.Models
{
  [Table("survey_question")]
  public record SurveyQuestion
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
    public Survey? Survey { get; set; }

    [ForeignKey("QuestionId")]
    public Question? Question { get; set; }
  }
}