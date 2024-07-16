using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DC.Models.Entities
{
  [Table("user_account")]
  public class UserAccount
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_name")]
    [MaxLength(50)]
    public string? Username { get; set; }

    [Column("password")]
    [MaxLength(50)]
    public string? Password { get; set; }

    [Column("email")]
    [MaxLength(50)]
    public string? Email { get; set; }

    [Column("role")]
    [MaxLength(50)]
    public string? Role { get; set; }
  }
}