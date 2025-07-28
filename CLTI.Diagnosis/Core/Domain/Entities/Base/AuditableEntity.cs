namespace CLTI.Diagnosis.Core.Domain.Entities.Base;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? ModifiedAt { get; set; }
    string? CreatedBy { get; set; }
    string? ModifiedBy { get; set; }
}
