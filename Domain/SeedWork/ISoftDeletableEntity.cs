namespace Domain.SeedWork
{
    public interface ISoftDeletableEntity
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
        Guid? DeletedBy { get; set; }
        void UndoDelete(); // Yanlışlıkla silinirse geri alabilmek için
    }
}
