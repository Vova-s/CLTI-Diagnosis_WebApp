using CLTI.Diagnosis.Core.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CLTI.Diagnosis.Core.Domain.Entities;

namespace CLTI.Diagnosis.Data;
using CLTI.Diagnosis.Infrastructure.Data.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    // System tables
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

    // CLTI specific tables
    public DbSet<CltiCase> CltiCases => Set<CltiCase>();
    public DbSet<CltiPhoto> CltiPhotos => Set<CltiPhoto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure CltiCase entity
        modelBuilder.Entity<CltiCase>(entity =>
        {
            entity.ToTable("u_clti");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Guid)
                .HasDefaultValueSql("NEWID()");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Configure the foreign key relationship for ClinicalStageEnumItem
            entity.HasOne(e => e.ClinicalStageEnumItem)
                .WithMany()
                .HasForeignKey(e => e.ClinicalStageWIfIEnumItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure the relationship with photos
            entity.HasMany(e => e.Photos)
                .WithOne(p => p.CltiCase)
                .HasForeignKey(p => p.CltiCaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CltiPhoto entity
        modelBuilder.Entity<CltiPhoto>(entity =>
        {
            entity.ToTable("u_clti_photos");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Guid)
                .HasDefaultValueSql("NEWID()");

            entity.Property(e => e.USOfLowerExtremityArteries)
                .HasColumnName("US_of_lower_extremity_arteries");
        });

        // Configure SysEnum
        modelBuilder.Entity<SysEnum>(entity =>
        {
            entity.ToTable("sys_enum");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Guid)
                .HasDefaultValueSql("NEWID()");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.OrderingType)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.OrderingTypeEditor)
                .IsRequired()
                .HasMaxLength(64);
        });

        // Configure SysEnumItem
        modelBuilder.Entity<SysEnumItem>(entity =>
        {
            entity.ToTable("sys_enum_item");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Guid)
                .HasDefaultValueSql("NEWID()");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Value)
                .HasMaxLength(255);

            entity.Property(e => e.Icon)
                .HasMaxLength(64);

            entity.Property(e => e.Color)
                .HasMaxLength(10);

            entity.HasOne(e => e.SysEnum)
                .WithMany(se => se.Items)
                .HasForeignKey(e => e.SysEnumId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure other system entities similarly...
        ConfigureSystemEntities(modelBuilder);
    }

    private void ConfigureSystemEntities(ModelBuilder modelBuilder)
    {
        // Configure SysUser
        modelBuilder.Entity<SysUser>(entity =>
        {
            entity.ToTable("sys_user");
            entity.Property(e => e.Guid).HasDefaultValueSql("NEWID()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure SysRole
        modelBuilder.Entity<SysRole>(entity =>
        {
            entity.ToTable("sys_role");
            entity.Property(e => e.Guid).HasDefaultValueSql("NEWID()");
        });

        // Configure SysRights
        modelBuilder.Entity<SysRights>(entity =>
        {
            entity.ToTable("sys_rights");
            entity.Property(e => e.Guid).HasDefaultValueSql("NEWID()");
        });

        // Configure SysUserRole
        modelBuilder.Entity<SysUserRole>(entity =>
        {
            entity.ToTable("sys_user_role");
            entity.HasOne(e => e.SysUser)
                .WithMany()
                .HasForeignKey(e => e.SysUserId);
            entity.HasOne(e => e.SysRole)
                .WithMany()
                .HasForeignKey(e => e.SysRoleId);
        });

        // Configure SysRoleRights
        modelBuilder.Entity<SysRoleRights>(entity =>
        {
            entity.ToTable("sys_role_rights");
            entity.HasOne(e => e.SysRole)
                .WithMany()
                .HasForeignKey(e => e.SysRoleId);
            entity.HasOne(e => e.SysRight)
                .WithMany()
                .HasForeignKey(e => e.SysRightId);
        });

        // Configure SysApiKey
        modelBuilder.Entity<SysApiKey>(entity =>
        {
            entity.ToTable("sys_api_key");
            entity.Property(e => e.Guid).HasDefaultValueSql("NEWID()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure SysLog
        modelBuilder.Entity<SysLog>(entity =>
        {
            entity.ToTable("sys_log");
            entity.Property(e => e.Date).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.LoggerNamespace).HasColumnName("Logger_namespace");
        });

        // Configure SysLicence
        modelBuilder.Entity<SysLicence>(entity =>
        {
            entity.ToTable("sys_licence");
            entity.Property(e => e.Guid).HasDefaultValueSql("NEWID()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure SysUserLicence
        modelBuilder.Entity<SysUserLicence>(entity =>
        {
            entity.ToTable("sys_user_licence");
            entity.Property(e => e.Guid).HasDefaultValueSql("NEWID()");
            entity.Property(e => e.AssignedDate).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Licence)
                .WithMany()
                .HasForeignKey(e => e.LicenceId);
        });
    }
}
