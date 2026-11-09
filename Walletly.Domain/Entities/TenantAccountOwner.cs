using SharedKernel;
using Walletly.Domain.Common;
using Walletly.Domain.Enums;
using Walletly.Domain.Errors;

namespace Walletly.Domain.Entities;

// Represents a dashboard operator/team member belonging to the Tenant organization.
public sealed class TenantAccountOwner : Entity
{
    private const int MaxNameLength = 100;

    private TenantAccountOwner()
    {
    }

    private TenantAccountOwner(
        Guid id,
        Guid tenantId,
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        TenantTeamRole role,
        DateTime createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
        IsActive = true;
        CreatedAt = createdAtUtc;
    }

    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public TenantTeamRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    public static Result<TenantAccountOwner> Create(
        Guid tenantId,
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        TenantTeamRole role,
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

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return TenantUserErrors.InvalidPasswordHash;
        }

        return new TenantAccountOwner(
            Guid.NewGuid(),
            tenantId,
            normalisedEmail,
            passwordHash,
            normalisedFirstName,
            normalisedLastName,
            role,
            createdAtUtc);
    }

    public Result RecordLogin(DateTime loggedInAtUtc)
    {
        if (!IsActive)
        {
            return TenantUserErrors.Inactive;
        }

        LastLoginAt = loggedInAtUtc;
        return Result.Success();
    }

    public void UpdateRole(TenantTeamRole newRole) => Role = newRole;
}
