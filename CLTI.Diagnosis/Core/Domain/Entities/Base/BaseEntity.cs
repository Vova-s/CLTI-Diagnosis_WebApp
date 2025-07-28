using System.ComponentModel.DataAnnotations;

namespace CLTI.Diagnosis.Core.Domain.Entities.Base;

public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }
    
    public Guid Guid { get; set; } = Guid.NewGuid();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ModifiedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
}
