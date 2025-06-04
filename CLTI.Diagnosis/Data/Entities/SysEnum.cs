using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CLTI.Diagnosis.Data.Entities;

[Table("sys_enum")]
public class SysEnum
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string OrderingType { get; set; } = string.Empty;

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Guid { get; set; }

    [Required]
    [MaxLength(64)]
    public string OrderingTypeEditor { get; set; } = string.Empty;

    public ICollection<SysEnumItem> Items { get; set; } = new List<SysEnumItem>();
}
