namespace NoticeSaaS.Domain.Enums;

public enum SyncJobStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    /// <summary>Portal login requires vault OTP; waiting for user submission.</summary>
    AwaitingOtp = 4
}
