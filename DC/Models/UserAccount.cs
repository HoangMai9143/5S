using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace DC.Models
{
  [Table("UserAccount")]
  public record UserAccount
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("ID")]
    public int ID { get; set; }

    [Column("userName")]
    [MaxLength(50)]
    public string? userName { get; set; }

    [Column("password")]
    [MaxLength(50)]
    public string? password { get; set; }

    [Column("department")]
    [MaxLength(50)]
    public string? department { get; set; }

    [Column("role")]
    [MaxLength(50)]
    public string? role { get; set; }
  }
}