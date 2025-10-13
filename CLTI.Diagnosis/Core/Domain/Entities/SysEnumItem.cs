using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CLTI.Diagnosis.Core.Domain.Entities;

[Table("sys_enum_item")]
public class SysEnumItem
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("SysEnum")]
    public int SysEnumId { get; set; }

    public SysEnum? SysEnum { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Value { get; set; }

    [MaxLength(64)]
    public string? Icon { get; set; }

    public int OrderIndex { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Guid { get; set; }

    [MaxLength(10)]
    public string? Color { get; set; }
}
