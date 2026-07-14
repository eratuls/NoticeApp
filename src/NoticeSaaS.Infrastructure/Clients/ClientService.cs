using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using NoticeSaaS.Application.Billing;
using NoticeSaaS.Application.Clients;
using NoticeSaaS.Application.Sync;
using NoticeSaaS.Domain.Entities;
using NoticeSaaS.Domain.Enums;
using NoticeSaaS.Infrastructure.Persistence;

namespace NoticeSaaS.Infrastructure.Clients;

public sealed class ClientService(
    NoticeSaaSDbContext db,
    IDataProtectionProvider dataProtectionProvider,
    IUsageLimitsService usageLimitsService,
    IIncomeTaxPortalClient incomeTaxPortalClient) : IClientService
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("NoticeSaaS.PortalCredentials.v1");

    public async Task<IReadOnlyList<ClientListItemDto>> ListAsync(
        Guid organizationId,
        ComplianceModule? module,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = db.Clients.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId);

        if (module is not null)
        {
            query = query.Where(c => c.Module == module.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.Name.Contains(term)
                || c.Pan.Contains(term)
                || (c.PortalUsername != null && c.PortalUsername.Contains(term)));
        }

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new ClientListItemDto(
                c.Id,
                c.Name,
                c.Pan,
                c.AadhaarMasked,
                c.CaPan,
                c.Module.ToString(),
                c.SyncFrequency.ToString(),
                c.PortalUsername,
                c.IsActive,
                c.CreatedAtUtc,
                c.LastSyncAtUtc,
                c.NextSyncAtUtc,
                c.Notices.Count,
                db.SyncJobs
                    .Where(j => j.ClientId == c.Id)
                    .OrderByDescending(j => j.CreatedAtUtc)
                    .Select(j => j.Status.ToString())
                    .FirstOrDefault(),
                db.SyncJobs
                    .Where(j => j.ClientId == c.Id)
                    .OrderByDescending(j => j.CreatedAtUtc)
                    .Select(j => j.ErrorMessage)
                    .FirstOrDefault(),
                db.SyncJobs
                    .Where(j => j.ClientId == c.Id)
                    .OrderByDescending(j => j.CreatedAtUtc)
                    .Select(j => (int?)j.NoticesUpserted)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async Task<CreateClientResult> CreateAsync(
        Guid organizationId,
        CreateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        var username = request.PortalUsername?.Trim() ?? string.Empty;
        var password = request.PortalPassword ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return CreateClientResult.Fail("Portal username and password are required.");
        }

        if (!Enum.TryParse<ComplianceModule>(request.Module, ignoreCase: true, out var module))
        {
            return CreateClientResult.Fail("Invalid module/category.");
        }

        if (!Enum.TryParse<SyncFrequency>(request.SyncFrequency, ignoreCase: true, out var syncFrequency))
        {
            return CreateClientResult.Fail("Invalid sync frequency.");
        }

        string name;
        string pan;
        string? aadhaarMasked = null;

        if (module == ComplianceModule.IncomeTax)
        {
            try
            {
                var profile = await incomeTaxPortalClient.LoginAndGetProfileAsync(
                    new PortalCredentialsRequest(username, password),
                    cancellationToken);

                name = profile.Name;
                pan = NormalizePan(profile.Pan);
                aadhaarMasked = profile.AadhaarMasked;
            }
            catch (Exception ex) when (ex is InvalidOperationException or PortalAuthException)
            {
                return CreateClientResult.Fail(ex.Message);
            }
        }
        else
        {
            name = request.Name?.Trim() ?? string.Empty;
            pan = NormalizePan(request.Pan);
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(pan))
            {
                return CreateClientResult.Fail("Name and PAN are required for this module.");
            }
        }

        if (pan.Length != 10)
        {
            return CreateClientResult.Fail("PAN must be exactly 10 characters.");
        }

        if (await db.Clients.AnyAsync(c => c.OrganizationId == organizationId && c.Pan == pan, cancellationToken))
        {
            return CreateClientResult.Fail("A client with this PAN already exists in the workspace.");
        }

        var seatCheck = await usageLimitsService.EnsureCanAddAssesseeAsync(organizationId, cancellationToken);
        if (!seatCheck.Allowed)
        {
            return CreateClientResult.Fail(seatCheck.Error ?? "Assessee limit reached.");
        }

        var now = DateTimeOffset.UtcNow;
        var client = new Client
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            Pan = pan,
            AadhaarMasked = aadhaarMasked,
            CaPan = string.IsNullOrWhiteSpace(request.CaPan) ? null : request.CaPan.Trim().ToUpperInvariant(),
            Module = module,
            SyncFrequency = syncFrequency,
            PortalUsername = username,
            IsActive = true,
            CreatedAtUtc = now,
            LastSyncAtUtc = null,
            NextSyncAtUtc = ComputeNextSync(now, syncFrequency)
        };

        var credential = new PortalCredential
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            Username = username,
            PasswordProtected = _protector.Protect(password),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Clients.Add(client);
        db.PortalCredentials.Add(credential);
        await db.SaveChangesAsync(cancellationToken);

        return CreateClientResult.Ok(new ClientListItemDto(
            client.Id,
            client.Name,
            client.Pan,
            client.AadhaarMasked,
            client.CaPan,
            client.Module.ToString(),
            client.SyncFrequency.ToString(),
            client.PortalUsername,
            client.IsActive,
            client.CreatedAtUtc,
            client.LastSyncAtUtc,
            client.NextSyncAtUtc,
            0));
    }

    public async Task<DeleteClientResult> DeleteAsync(
        Guid organizationId,
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var client = await db.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.OrganizationId == organizationId, cancellationToken);

        if (client is null)
        {
            return DeleteClientResult.Fail("Client not found.");
        }

        var noticeIds = await db.Notices
            .Where(n => n.ClientId == clientId)
            .Select(n => n.Id)
            .ToListAsync(cancellationToken);

        // Reminders use NoAction FKs — remove before notices/client cascade.
        await db.Reminders
            .Where(r => r.ClientId == clientId || (r.NoticeId != null && noticeIds.Contains(r.NoticeId.Value)))
            .ExecuteDeleteAsync(cancellationToken);

        // Ledger keeps billing history but must not block SyncJob cascade.
        var syncJobIds = await db.SyncJobs
            .Where(j => j.ClientId == clientId)
            .Select(j => j.Id)
            .ToListAsync(cancellationToken);

        if (syncJobIds.Count > 0)
        {
            await db.SyncCreditLedger
                .Where(e => e.SyncJobId != null && syncJobIds.Contains(e.SyncJobId.Value))
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(e => e.SyncJobId, (Guid?)null),
                    cancellationToken);
        }

        // Cascades: PortalCredential, Notices (+ comments/status events), SyncJobs (+ logs).
        db.Clients.Remove(client);
        await db.SaveChangesAsync(cancellationToken);

        return DeleteClientResult.Ok();
    }

    public static DateTimeOffset ComputeNextSync(DateTimeOffset from, SyncFrequency frequency) =>
        frequency switch
        {
            SyncFrequency.Daily => from.AddDays(1),
            SyncFrequency.Weekly => from.AddDays(7),
            SyncFrequency.Midweek => from.AddDays(3),
            SyncFrequency.Fortnightly => from.AddDays(14),
            SyncFrequency.Monthly => from.AddMonths(1),
            _ => from.AddDays(7)
        };

    private static string NormalizePan(string? pan) =>
        (pan ?? string.Empty).Trim().ToUpperInvariant();
}
