using NoticeSaaS.Application.Dashboard;

namespace NoticeSaaS.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryResponse?> GetSummaryAsync(
        Guid organizationId,
        DashboardSummaryRequest request,
        CancellationToken cancellationToken = default);
}
