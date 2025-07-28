using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CLTI.Diagnosis.Data.Entities;

[Table("sys_user_role")]
public class SysUserRole
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("SysUser")]
    public int SysUserId { get; set; }

    public SysUser? SysUser { get; set; }

    [ForeignKey("SysRole")]
    public int SysRoleId { get; set; }

    public SysRole? SysRole { get; set; }
}
