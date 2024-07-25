using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace DC.Models
{
  [Table("Question")]
  public class QuestionModel
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("question_context")]
    [MaxLength(255)]
    public string? QuestionContext { get; set; }

    // Navigation property for answers
    public virtual ICollection<AnswerModel> Answers { get; set; } = new List<AnswerModel>();
  }
}