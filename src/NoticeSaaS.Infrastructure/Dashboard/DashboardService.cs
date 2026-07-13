using Microsoft.EntityFrameworkCore;
using NoticeSaaS.Application.Dashboard;
using NoticeSaaS.Domain.Enums;
using NoticeSaaS.Infrastructure.Persistence;

namespace NoticeSaaS.Infrastructure.Dashboard;

public sealed class DashboardService(NoticeSaaSDbContext db) : IDashboardService
{
    public async Task<DashboardSummaryResponse?> GetSummaryAsync(
        Guid organizationId,
        DashboardSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Organizations.AnyAsync(o => o.Id == organizationId, cancellationToken))
        {
            return null;
        }

        var module = ParseModule(request.Module);
        var periodStart = GetPeriodStartUtc(request.Period);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var clientsQuery = db.Clients.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId && c.Module == module);

        var clientTotal = await clientsQuery.CountAsync(cancellationToken);
        var clientsAdded = await clientsQuery.CountAsync(c => c.CreatedAtUtc >= periodStart, cancellationToken);

        var members = await db.OrganizationMembers
            .AsNoTracking()
            .Where(m => m.OrganizationId == organizationId)
            .Join(db.Users.AsNoTracking(), m => m.UserId, u => u.Id, (m, u) => u)
            .Distinct()
            .ToListAsync(cancellationToken);

        var teamActive = members.Count(u => u.IsActive);
        var teamInactive = members.Count(u => !u.IsActive);

        var notices = await db.Notices.AsNoTracking()
            .Where(n => n.OrganizationId == organizationId && n.Module == module)
            .Select(n => new { n.Status, n.ResponseDueDate })
            .ToListAsync(cancellationToken);

        var newCount = notices.Count(n => n.Status == NoticeWorkflowStatus.New);
        var ongoingCount = notices.Count(n =>
            n.Status is NoticeWorkflowStatus.Open
                or NoticeWorkflowStatus.InProgress
                or NoticeWorkflowStatus.Replied);
        var closedCount = notices.Count(n => n.Status == NoticeWorkflowStatus.Closed);
        var overdueCount = notices.Count(n =>
            n.Status != NoticeWorkflowStatus.Closed
            && n.ResponseDueDate is not null
            && n.ResponseDueDate.Value < today);

        return new DashboardSummaryResponse(
            Module: module.ToString(),
            Period: string.IsNullOrWhiteSpace(request.Period) ? "Monthly" : request.Period,
            Clients: new CountDelta(clientTotal, clientsAdded),
            Team: new TeamSummary(members.Count, teamActive, teamInactive),
            Notices: new NoticeSummary(notices.Count, notices.Count, 0),
            Tasks: new TaskBuckets(newCount, ongoingCount, closedCount, overdueCount));
    }

    private static ComplianceModule ParseModule(string? module) =>
        Enum.TryParse<ComplianceModule>(module, ignoreCase: true, out var parsed)
            ? parsed
            : ComplianceModule.IncomeTax;

    private static DateTimeOffset GetPeriodStartUtc(string? period)
    {
        var now = DateTimeOffset.UtcNow;
        return period?.Equals("Weekly", StringComparison.OrdinalIgnoreCase) == true
            ? now.AddDays(-7)
            : new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
    }
}
