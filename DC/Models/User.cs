using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DC.Models
{
  [Table("user")]
  public record User
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int id { get; set; }

    [Column("username")]
    [MaxLength(255)]
    public string? Username { get; set; }

    [Column("password")]
    [MaxLength(255)]
    public string? Password { get; set; }

    [Column("department")]
    [MaxLength(255)]
    public string? Department { get; set; }

    [Column("role")]
    [MaxLength(255)]
    public string? Role { get; set; }

    [Column("created_date")]
    public DateTime CreatedAt { get; set; }

    [Column("isActive")]
    public bool IsActive { get; set; }
  }
}