using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace DC.Models
{
  [Table("QuestionAnswer")]
  public class QuestionAnswerModel
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("question_id")]
    [MaxLength(255)]
    public int? QuestionId { get; set; }

    [Column("answer_id")]
    [MaxLength(255)]
    public int? AnswerId { get; set; }

    [ForeignKey("QuestionId")]
    public QuestionModel? Question { get; set; }
    [ForeignKey("AnswerId")]
    public AnswerModel? Answer { get; set; }

  }
}