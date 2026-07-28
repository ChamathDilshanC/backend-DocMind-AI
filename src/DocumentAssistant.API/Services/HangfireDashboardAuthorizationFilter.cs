using Hangfire.Dashboard;

namespace DocumentAssistant.API.Services;

/// <summary>Dev-time convenience: gates /hangfire to the Admin role. A real deployment should put this behind a VPN/reverse-proxy auth too.</summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsInRole("Admin");
    }
}
