using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CLTI.Diagnosis.Core.Domain.Entities;

[Table("sys_licence")]
public class SysLicence
{
    [Key]
    public int Id { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Guid { get; set; }

    [Required]
    [MaxLength(255)]
    public string LicenceKey { get; set; } = string.Empty;

    [ForeignKey("LicenceTypeEnumItem")]
    public int LicenceTypeEnumItemId { get; set; }

    public SysEnumItem? LicenceTypeEnumItem { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [ForeignKey("StatusEnumItem")]
    public int StatusEnumItemId { get; set; }

    public SysEnumItem? StatusEnumItem { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
