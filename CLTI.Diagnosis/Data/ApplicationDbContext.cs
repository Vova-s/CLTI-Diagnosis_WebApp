using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CLTI.Diagnosis.Data.Entities;

namespace CLTI.Diagnosis.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<SysEnum> SysEnums => Set<SysEnum>();
    public DbSet<SysEnumItem> SysEnumItems => Set<SysEnumItem>();
    public DbSet<SysUser> SysUsers => Set<SysUser>();
    public DbSet<SysRole> SysRoles => Set<SysRole>();
    public DbSet<SysRights> SysRights => Set<SysRights>();
    public DbSet<SysUserRole> SysUserRoles => Set<SysUserRole>();
    public DbSet<SysRoleRights> SysRoleRights => Set<SysRoleRights>();
    public DbSet<SysApiKey> SysApiKeys => Set<SysApiKey>();
    public DbSet<SysLog> SysLogs => Set<SysLog>();
    public DbSet<SysLicence> SysLicences => Set<SysLicence>();
    public DbSet<SysUserLicence> SysUserLicences => Set<SysUserLicence>();
    public DbSet<CltiCase> CltiCases => Set<CltiCase>();
    public DbSet<CltiPhoto> CltiPhotos => Set<CltiPhoto>();
}
