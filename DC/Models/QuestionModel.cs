using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace DC.Models
{

  public enum AnswerType
  {
    SingleChoice = 1,
    MultipleChoice = 2
  }

  [Table("Question")]
  public class QuestionModel
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("question_context")]
    [MaxLength(500)]
    public string QuestionContext { get; set; }

    [Required]
    [Column("answer_type")]
    public AnswerType AnswerType { get; set; }

    public virtual ICollection<AnswerModel> Answers { get; set; }
  }
}