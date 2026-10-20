using System.Text.RegularExpressions;
using SharedKernel;
using Walletly.Domain.Errors;

namespace Walletly.Domain.Entities;

public sealed partial class TenantUser : Entity
{
    private const int MaxEmailLength = 255;
    private const int MaxNameLength = 100;

    private TenantUser()
    {
    }

    private TenantUser(Guid id, Guid tenantId, string email, string passwordHash, string firstName, string lastName, DateTime createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        IsActive = true;
        CreatedAt = createdAtUtc;
    }

    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    public static Result<TenantUser> Create(Guid tenantId, string email, string passwordHash, string firstName, string lastName, DateTime createdAtUtc)
    {
        var normalisedEmail = email.Trim().ToLowerInvariant();
        if (normalisedEmail.Length is 0 or > MaxEmailLength || !EmailPattern().IsMatch(normalisedEmail))
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

        return new TenantUser(Guid.NewGuid(), tenantId, normalisedEmail, passwordHash, normalisedFirstName, normalisedLastName, createdAtUtc);
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
}
