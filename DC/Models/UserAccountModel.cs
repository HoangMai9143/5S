using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DC.Models
{
  [Table("UserAccountModel")]
  public record UserAccountModel
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("username")]
    [MaxLength(255)]
    public string? Username { get; set; }

    [Column("password")]
    [MaxLength(255)]
    public string? Password { get; set; }

    [Column("role")]
    [MaxLength(255)]
    public string? Role { get; set; }

    [Column("isActive")]
    public bool IsActive { get; set; }
  }
}