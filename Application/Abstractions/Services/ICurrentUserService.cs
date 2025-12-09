namespace Application.Abstractions.Services
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        Guid? CompanyId { get; }
    }
}
