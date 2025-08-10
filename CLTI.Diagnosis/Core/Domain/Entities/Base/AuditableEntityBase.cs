namespace CLTI.Diagnosis.Core.Domain.Entities.Base;
using CLTI.Diagnosis.Core.Domain.Entities.Base;

public abstract class AuditableEntity : BaseEntity, IAuditableEntity, ISoftDeletable
{
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
