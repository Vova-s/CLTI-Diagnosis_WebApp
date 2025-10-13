using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CLTI.Diagnosis.Core.Domain.Entities.Base;
using CLTI.Diagnosis.Core.Domain.Entities.ValueObjects;

namespace CLTI.Diagnosis.Core.Domain.Entities;


[Table("u_clti")]
public sealed class CltiCase : BaseEntity, IValidatableObject
{
    // Гемодинамічні показники
    [Required]
    [Range(0.0, double.MaxValue)]
    public double AbiKpi { get; set; }
    
    [Range(0.0, double.MaxValue)]
    public double? FbiPpi { get; set; }

    // WiFI критерії
    public WifiCriteria WifiCriteria { get; set; } = new();

    // Клінічна стадія за WiFI
    [Required]
    [ForeignKey("ClinicalStageEnumItem")]
    public int ClinicalStageWIfIEnumItemId { get; set; }
    
    public SysEnumItem? ClinicalStageEnumItem { get; set; }

    // CRAB оцінка (періпроцедуральна смертність)
    [Required]
    [Range(0, int.MaxValue)]
    public int CrabPoints { get; set; }

    // 2YLE оцінка (дворічна виживаність)
    [Required]
    [Range(0.0, 100.0)]
    public double TwoYLE { get; set; }

    // GLASS критерії
    public GlassCriteria GlassCriteria { get; set; } = new();

    // Навігаційні властивості
    public ICollection<CltiPhoto> Photos { get; private set; } = new List<CltiPhoto>();

    // Конструктор
    public CltiCase()
    {
        WifiCriteria = new WifiCriteria();
        GlassCriteria = new GlassCriteria();
    }

    // Методи для роботи з колекцією фото
    public void AddPhoto(CltiPhoto photo)
    {
        Photos.Add(photo);
    }

    public void RemovePhoto(CltiPhoto photo)
    {
        Photos.Remove(photo);
    }

    // Валідація
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AbiKpi <= 0)
        {
            yield return new ValidationResult(
                "AbiKpi must be greater than 0",
                new[] { nameof(AbiKpi) }
            );
        }

        if (FbiPpi.HasValue && FbiPpi <= 0)
        {
            yield return new ValidationResult(
                "FbiPpi must be greater than 0",
                new[] { nameof(FbiPpi) }
            );
        }

        if (TwoYLE < 0 || TwoYLE > 100)
        {
            yield return new ValidationResult(
                "TwoYLE must be between 0 and 100",
                new[] { nameof(TwoYLE) }
            );
        }
    }
}
