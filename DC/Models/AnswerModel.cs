using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DC.Models
{
  [Table("Answer")]
  public class AnswerModel
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("answer_text")]
    [MaxLength(500)]
    public string AnswerText { get; set; }

    [Required]
    [Column("points")]
    public int Points { get; set; }

    [Required]
    [Column("question_id")]
    public int QuestionId { get; set; }

    [ForeignKey("QuestionId")]
    public virtual QuestionModel Question { get; set; }
  }
}