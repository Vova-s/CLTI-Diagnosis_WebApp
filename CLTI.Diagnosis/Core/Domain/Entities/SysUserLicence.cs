using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CLTI.Diagnosis.Core.Domain.Entities;

[Table("sys_user_licence")]
public class SysUserLicence
{
    [Key]
    public int Id { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Guid { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }

    public SysUser? User { get; set; }

    [ForeignKey("Licence")]
    public int LicenceId { get; set; }

    public SysLicence? Licence { get; set; }

    public DateTime AssignedDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [ForeignKey("StatusEnumItem")]
    public int StatusEnumItemId { get; set; }

    public SysEnumItem? StatusEnumItem { get; set; }
}
