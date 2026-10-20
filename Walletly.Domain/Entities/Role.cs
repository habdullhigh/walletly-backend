using System.Text.RegularExpressions;
using SharedKernel;
using Walletly.Domain.Errors;

namespace Walletly.Domain.Entities;

public sealed partial class Role : Entity
{
    private const int MaxCodeLength = 50;
    private const int MaxNameLength = 100;

    private Role()
    {
    }

    private Role(Guid id, string code, string name)
        : base(id)
    {
        Code = code;
        Name = name;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    [GeneratedRegex(@"^[a-z][a-z0-9_]*$")]
    private static partial Regex CodePattern();

    public static Result<Role> Create(string code, string name)
    {
        var normalisedCode = code.Trim().ToLowerInvariant();
        if (normalisedCode.Length is 0 or > MaxCodeLength || !CodePattern().IsMatch(normalisedCode))
        {
            return RoleErrors.InvalidCode;
        }

        var normalisedName = name.Trim();
        if (normalisedName.Length is 0 or > MaxNameLength)
        {
            return RoleErrors.InvalidName;
        }

        return new Role(Guid.NewGuid(), normalisedCode, normalisedName);
    }
}
