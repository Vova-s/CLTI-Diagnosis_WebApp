namespace CLTI.Diagnosis.Core.Domain.Entities.Base;

public interface IEntity
{
    int Id { get; set; }
    Guid Guid { get; set; }
}

public interface IEntity<TKey> where TKey : IEquatable<TKey>
{
    TKey Id { get; set; }
}
