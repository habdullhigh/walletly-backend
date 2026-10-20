using System.Text.RegularExpressions;
using SharedKernel;
using Walletly.Domain.Errors;

namespace Walletly.Domain.Entities;

public sealed partial class PlatformUser : Entity
{
    private const int MaxEmailLength = 255;
    private const int MaxNameLength = 100;

    private PlatformUser()
    {
    }

    private PlatformUser(Guid id, string email, string passwordHash, string firstName, string lastName, DateTime createdAtUtc)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        IsActive = true;
        CreatedAt = createdAtUtc;
    }

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

    public static Result<PlatformUser> Create(string email, string passwordHash, string firstName, string lastName, DateTime createdAtUtc)
    {
        var normalisedEmail = email.Trim().ToLowerInvariant();
        if (normalisedEmail.Length is 0 or > MaxEmailLength || !EmailPattern().IsMatch(normalisedEmail))
        {
            return PlatformUserErrors.InvalidEmail;
        }

        var normalisedFirstName = firstName.Trim();
        if (normalisedFirstName.Length is 0 or > MaxNameLength)
        {
            return PlatformUserErrors.InvalidFirstName;
        }

        var normalisedLastName = lastName.Trim();
        if (normalisedLastName.Length is 0 or > MaxNameLength)
        {
            return PlatformUserErrors.InvalidLastName;
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return PlatformUserErrors.InvalidPasswordHash;
        }

        return new PlatformUser(Guid.NewGuid(), normalisedEmail, passwordHash, normalisedFirstName, normalisedLastName, createdAtUtc);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public Result RecordLogin(DateTime loggedInAtUtc)
    {
        if (!IsActive)
        {
            return PlatformUserErrors.Inactive;
        }

        LastLoginAt = loggedInAtUtc;
        return Result.Success();
    }
}
