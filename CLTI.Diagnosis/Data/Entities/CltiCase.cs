using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CLTI.Diagnosis.Data.Entities;

[Table("u_clti")]
public class CltiCase
{
    [Key]
    public int Id { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Guid { get; set; }

    public DateTime CreatedAt { get; set; }

    public double AbiKpi { get; set; }

    public double? FbiPpi { get; set; }

    public bool W1 { get; set; }
    public bool W2 { get; set; }
    public bool W3 { get; set; }

    public bool I0 { get; set; }
    public bool I1 { get; set; }
    public bool I2 { get; set; }
    public bool I3 { get; set; }

    public bool FI0 { get; set; }
    public bool FI1 { get; set; }
    public bool FI2 { get; set; }
    public bool FI3 { get; set; }

    [ForeignKey("ClinicalStageEnumItem")]
    public int ClinicalStageWIfIEnumItemId { get; set; }

    public SysEnumItem? ClinicalStageEnumItem { get; set; }

    public int CrabPoints { get; set; }

    public double TwoYLE { get; set; }

    public bool GlassAidI { get; set; }
    public bool GlassAidII { get; set; }
    public bool GlassAidA { get; set; }
    public bool GlassAidB { get; set; }

    public int GlassFps { get; set; }
    public int GlassIps { get; set; }

    public bool GlassIid { get; set; }
    public bool GlassIidI { get; set; }
    public bool GlassIidII { get; set; }
    public bool GlassIidIII { get; set; }

    public bool GlassImdP0 { get; set; }
    public bool GlassImdP1 { get; set; }
    public bool GlassImdP2 { get; set; }

    public ICollection<CltiPhoto> Photos { get; set; } = new List<CltiPhoto>();
}
