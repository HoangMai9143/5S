using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace DC.Models
{
  [Table("UserAccount")]
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
    [DefaultValue("User")]
    public string? Role { get; set; } = "User";

    [Column("isActive", TypeName = "bit")]
    [DefaultValue(true)]
    public bool IsActive { get; set; } = true;
  }
}