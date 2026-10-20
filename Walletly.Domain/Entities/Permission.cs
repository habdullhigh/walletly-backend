using System.Text.RegularExpressions;
using SharedKernel;
using Walletly.Domain.Errors;

namespace Walletly.Domain.Entities;

public sealed partial class Permission : Entity
{
    private const int MaxCodeLength = 100;

    private Permission()
    {
    }

    private Permission(Guid id, string code)
        : base(id)
    {
        Code = code;
    }

    public string Code { get; private set; } = string.Empty;

    [GeneratedRegex(@"^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)*$")]
    private static partial Regex CodePattern();

    public static Result<Permission> Create(string code)
    {
        var normalisedCode = code.Trim().ToLowerInvariant();
        if (normalisedCode.Length is 0 or > MaxCodeLength || !CodePattern().IsMatch(normalisedCode))
        {
            return PermissionErrors.InvalidCode;
        }

        return new Permission(Guid.NewGuid(), normalisedCode);
    }
}
