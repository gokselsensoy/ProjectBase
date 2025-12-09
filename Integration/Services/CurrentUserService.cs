using Application.Abstractions.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Integration.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid UserId
        {
            get
            {
                var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value
                              ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return Guid.TryParse(idClaim, out var guid) ? guid : Guid.Empty;
            }
        }

        public Guid? CompanyId
        {
            get
            {
                var companyClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("company_id")?.Value;
                return Guid.TryParse(companyClaim, out var guid) ? guid : null;
            }
        }
    }
}
