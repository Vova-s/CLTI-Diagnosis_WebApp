using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CLTI.Diagnosis.Core.Domain.Entities;

[Table("sys_role_rights")]
public class SysRoleRights
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("SysRole")]
    public int SysRoleId { get; set; }

    public SysRole? SysRole { get; set; }

    [ForeignKey("SysRight")]
    public int SysRightId { get; set; }

    public SysRights? SysRight { get; set; }
}
