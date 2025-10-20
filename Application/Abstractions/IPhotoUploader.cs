namespace Application.Abstractions
{
    public interface IPhotoUploader
    {
        Task UploadOrderPhotoAsync(Guid orderId, string photoBase64, Guid customerId);
    }
}
