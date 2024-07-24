using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DC.Models
{
  public enum QuestionType
  {
    ShortAnswer = 1,
    MultipleChoice = 2,
    Checkboxes = 3,
    Dropdown = 4,
    FileUpload = 5
  }

  [Table("Question")]
  public class QuestionModel
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("question_context")]
    [MaxLength(255)]
    public string? QuestionContext { get; set; }

    [Column("question_type")]
    public QuestionType QuestionType { get; set; }
  }
}