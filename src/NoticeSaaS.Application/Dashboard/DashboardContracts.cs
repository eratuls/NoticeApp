namespace NoticeSaaS.Application.Dashboard;

public sealed record DashboardSummaryRequest(string Module = "IncomeTax", string Period = "Monthly");

public sealed record CountDelta(int Total, int AddedInPeriod);

public sealed record TeamSummary(int Total, int Active, int Inactive);

public sealed record NoticeSummary(int Total, int SelfPan, int OtherPan);

public sealed record TaskBuckets(int New, int Ongoing, int Closed, int Overdue);

public sealed record DashboardSummaryResponse(
    string Module,
    string Period,
    CountDelta Clients,
    TeamSummary Team,
    NoticeSummary Notices,
    TaskBuckets Tasks);
