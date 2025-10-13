using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CLTI.Diagnosis.Core.Domain.Entities;

[Table("sys_refresh_token")]
public class SysRefreshToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    [ForeignKey("SysUser")]
    public int UserId { get; set; }

    public SysUser User { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public bool IsUsed { get; set; }

    [MaxLength(500)]
    public string? ReplacedByToken { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Guid { get; set; }
}
