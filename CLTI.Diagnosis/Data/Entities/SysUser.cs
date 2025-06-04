using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CLTI.Diagnosis.Data.Entities;

[Table("sys_user")]
public class SysUser
{
    [Key]
    public int Id { get; set; }

    [MaxLength(50)]
    public string? TitleBeforeName { get; set; }

    [MaxLength(50)]
    public string? FirstName { get; set; }

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? TitleAfterName { get; set; }

    [Required]
    [MaxLength(255)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    [ForeignKey("StatusEnumItem")]
    public int StatusEnumItemId { get; set; }

    public SysEnumItem? StatusEnumItem { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Guid { get; set; }
}
