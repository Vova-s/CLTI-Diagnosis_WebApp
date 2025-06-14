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
    public Guid Guid { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Гемодинамічні показники
    public double AbiKpi { get; set; }
    public double? FbiPpi { get; set; }

    // WiFI W критерії (рівень втрати тканини)
    public bool W1 { get; set; }
    public bool W2 { get; set; }
    public bool W3 { get; set; }

    // WiFI I критерії (рівень ішемії)
    public bool I0 { get; set; }
    public bool I1 { get; set; }
    public bool I2 { get; set; }
    public bool I3 { get; set; }

    // WiFI fI критерії (рівень інфекції стопи)
    public bool FI0 { get; set; }
    public bool FI1 { get; set; }
    public bool FI2 { get; set; }
    public bool FI3 { get; set; }

    // Клінічна стадія за WiFI
    [ForeignKey("ClinicalStageEnumItem")]
    public int ClinicalStageWIfIEnumItemId { get; set; }
    public virtual SysEnumItem? ClinicalStageEnumItem { get; set; }

    // CRAB оцінка (періпроцедуральна смертність)
    public int CrabPoints { get; set; }

    // 2YLE оцінка (дворічна виживаність)
    public double TwoYLE { get; set; }

    // GLASS аорто-ілеальний сегмент
    public bool GlassAidI { get; set; }       // Стадія I
    public bool GlassAidII { get; set; }      // Стадія II
    public bool GlassAidA { get; set; }       // Підстадія A
    public bool GlassAidB { get; set; }       // Підстадія B

    // GLASS стегново-підколінний сегмент (0-4)
    public int GlassFps { get; set; }

    // GLASS інфрапоплітеальний сегмент (0-4)  
    public int GlassIps { get; set; }

    // GLASS фінальна інфраінгвінальна стадія
    public bool GlassIid { get; set; }        // Чи визначена стадія
    public bool GlassIidI { get; set; }       // Стадія I
    public bool GlassIidII { get; set; }      // Стадія II
    public bool GlassIidIII { get; set; }     // Стадія III

    // Дескриптор підкісточкової (стопної) хвороби
    public bool GlassImdP0 { get; set; }      // P0
    public bool GlassImdP1 { get; set; }      // P1
    public bool GlassImdP2 { get; set; }      // P2

    // Навігаційні властивості
    public virtual ICollection<CltiPhoto> Photos { get; set; } = new List<CltiPhoto>();
}