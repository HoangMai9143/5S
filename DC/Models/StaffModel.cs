using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DC.Models
{
  [Table("StaffModel")]
  public class StaffModel
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("full_name")]
    [MaxLength(255)]
    public string FullName { get; set; }

    [Column("department")]
    [MaxLength(255)]
    public string Department { get; set; }

    [Column("isActive")]
    public bool IsActive { get; set; }
  }
}