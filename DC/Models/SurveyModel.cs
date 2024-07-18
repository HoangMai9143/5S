using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;


namespace DC.Models
{
  [Table("Survey")]
  public record SurveyModel
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("start_date", TypeName = "datetime")]
    public DateTime StartDate { get; set; }

    [Column("end_date", TypeName = "datetime")]
    public DateTime EndDate { get; set; }

    [Column("created_date", TypeName = "datetime")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("isActive", TypeName = "bit")]
    [DefaultValue(true)]
    public bool IsActive { get; set; } = true;
  }
}