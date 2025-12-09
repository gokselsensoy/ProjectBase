namespace Domain.SeedWork
{
    public interface IAuditableEntity
    {
        DateTime CreatedAt { get; set; }
        Guid? CreatedBy { get; set; }
        DateTime? LastModifiedAt { get; set; }
        Guid? LastModifiedBy { get; set; }
    }
}
