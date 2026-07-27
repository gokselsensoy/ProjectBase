using Hangfire.Dashboard;

namespace WebApi.Filters
{
    /// <summary>
    /// Minimum viable lockdown for /hangfire: requires an authenticated user. Extend with a role/claim
    /// check (e.g. "admin" role) once this template is specialized for a real project — background
    /// job state and the ability to re-trigger jobs should not be exposed to every logged-in user forever.
    /// </summary>
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.User?.Identity?.IsAuthenticated == true;
        }
    }
}
