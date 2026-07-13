namespace NoticeSaaS.Domain.Enums;

public enum ComplianceModule
{
    IncomeTax = 0,
    Gst = 1,
    Itr = 2,
    InsightReport = 3
}

public enum NoticeWorkflowStatus
{
    New = 0,
    Open = 1,
    InProgress = 2,
    Replied = 3,
    Closed = 4
}

public enum SyncFrequency
{
    Daily = 0,
    Weekly = 1,
    Midweek = 2,
    Fortnightly = 3,
    Monthly = 4
}

public enum NoticeKind
{
    Notice = 0,
    DirectOrder = 1,
    Manual = 2,
    CaseStatus = 3
}

public enum ReminderPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}
