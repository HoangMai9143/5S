using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DC.Models
{
  [Table("SurveyResult")]
  public record SurveyResultModel
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("survey_id")]
    public int SurveyId { get; set; }

    [Column("staff_id")]
    public int StaffId { get; set; }

    [Column("final_grade")]
    public int FinalGrade { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [ForeignKey("SurveyId")]
    public SurveyModel? Survey { get; set; }

    [ForeignKey("StaffId")]
    public StaffModel? Staff { get; set; }
  }
}