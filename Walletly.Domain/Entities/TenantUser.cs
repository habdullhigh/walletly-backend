using SharedKernel;
using Walletly.Domain.Common;
using Walletly.Domain.Errors;

namespace Walletly.Domain.Entities;

// Represents an end-user / customer created and managed by the tenant within a specific environment (Test or Live).
// TenantUsers are purely managed via API keys and never log in to Walletly directly.
public sealed class TenantUser : Entity
{
    private const int MaxNameLength = 100;
    private const int MaxExternalReferenceLength = 100;

    private TenantUser()
    {
    }

    private TenantUser(
        Guid id,
        Guid tenantEnvironmentId,
        string email,
        string firstName,
        string lastName,
        Guid? tenantRoleId,
        string? externalReference,
        DateTime createdAtUtc)
        : base(id)
    {
        TenantEnvironmentId = tenantEnvironmentId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        TenantRoleId = tenantRoleId;
        ExternalReference = externalReference;
        IsActive = true;
        CreatedAt = createdAtUtc;
    }

    public Guid TenantEnvironmentId { get; private set; }
    public Guid? TenantRoleId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? ExternalReference { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Result<TenantUser> Create(
        Guid tenantEnvironmentId,
        string email,
        string firstName,
        string lastName,
        Guid? tenantRoleId,
        string? externalReference,
        DateTime createdAtUtc)
    {
        var normalisedEmail = EmailAddress.Normalise(email);
        if (!EmailAddress.IsValid(normalisedEmail))
        {
            return TenantUserErrors.InvalidEmail;
        }

        var normalisedFirstName = firstName.Trim();
        if (normalisedFirstName.Length is 0 or > MaxNameLength)
        {
            return TenantUserErrors.InvalidFirstName;
        }

        var normalisedLastName = lastName.Trim();
        if (normalisedLastName.Length is 0 or > MaxNameLength)
        {
            return TenantUserErrors.InvalidLastName;
        }

        var cleanRef = externalReference?.Trim();
        if (cleanRef is not null && cleanRef.Length > MaxExternalReferenceLength)
        {
            cleanRef = cleanRef[..MaxExternalReferenceLength];
        }

        return new TenantUser(
            Guid.NewGuid(),
            tenantEnvironmentId,
            normalisedEmail,
            normalisedFirstName,
            normalisedLastName,
            tenantRoleId,
            cleanRef,
            createdAtUtc);
    }

    public void AssignRole(Guid? roleId) => TenantRoleId = roleId;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public Result UpdateDetails(string firstName, string lastName, string? externalReference)
    {
        var normalisedFirstName = firstName.Trim();
        if (normalisedFirstName.Length is 0 or > MaxNameLength)
        {
            return TenantUserErrors.InvalidFirstName;
        }

        var normalisedLastName = lastName.Trim();
        if (normalisedLastName.Length is 0 or > MaxNameLength)
        {
            return TenantUserErrors.InvalidLastName;
        }

        var cleanRef = externalReference?.Trim();
        if (cleanRef is not null && cleanRef.Length > MaxExternalReferenceLength)
        {
            cleanRef = cleanRef[..MaxExternalReferenceLength];
        }

        FirstName = normalisedFirstName;
        LastName = normalisedLastName;
        ExternalReference = cleanRef;
        return Result.Success();
    }
}
