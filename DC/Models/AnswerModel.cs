using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DC.Models
{
  public enum AnswerType
  {
    SingleChoice = 1,
    MultipleChoice = 2
  }

  [Table("Answers")]
  public class AnswerModel
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("question_id")]

    [ForeignKey("question")]
    public int QuestionId { get; set; }

    [Required]
    [Column("answer_text", TypeName = "nvarchar(500)")]
    public string AnswerText { get; set; }

    [Required]
    [Column("points")]
    public int Points { get; set; }

    [Required]
    [Column("answer_type")]
    public AnswerType AnswerType { get; set; }

    // Navigation property
    public virtual QuestionModel Question { get; set; }
  }
}