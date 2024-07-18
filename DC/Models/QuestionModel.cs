using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DC.Models
{
  [Table("QuestionModel")]
  public record QuestionModel
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("question_context")]
    [MaxLength(255)]
    public string? QuestionContext { get; set; }
  }
}