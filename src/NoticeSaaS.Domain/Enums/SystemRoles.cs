namespace NoticeSaaS.Domain.Enums;

public static class SystemRoles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
    public const string ClientViewer = "ClientViewer";

    public static readonly string[] All =
    [
        Owner,
        Admin,
        Manager,
        Staff,
        ClientViewer
    ];
}
