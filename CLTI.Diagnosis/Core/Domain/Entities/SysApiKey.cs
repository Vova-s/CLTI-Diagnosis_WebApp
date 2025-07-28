using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CLTI.Diagnosis.Data.Entities;

[Table("sys_api_key")]
public class SysApiKey
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string ApiKey { get; set; } = string.Empty;

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Guid { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    [ForeignKey("StatusEnumItem")]
    public int StatusEnumItemId { get; set; }

    public SysEnumItem? StatusEnumItem { get; set; }
}
