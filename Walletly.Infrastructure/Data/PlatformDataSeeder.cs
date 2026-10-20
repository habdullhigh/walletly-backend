using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Walletly.Application.Common.Interfaces;
using Walletly.Domain.Entities;

namespace Walletly.Infrastructure.Data;

public class PlatformDataSeeder
{
    private readonly WalletlyDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PlatformDataSeeder> _logger;

    public PlatformDataSeeder(
        WalletlyDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<PlatformDataSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var adminEmail = _configuration["PlatformAdmin:Email"] ?? "walletly.admin@yopmail.com";
        var adminPassword = _configuration["PlatformAdmin:Password"] ?? "AdminPassword123!";
        var adminFirstName = _configuration["PlatformAdmin:FirstName"] ?? "Platform";
        var adminLastName = _configuration["PlatformAdmin:LastName"] ?? "Administrator";

        var normalizedEmail = adminEmail.Trim().ToLowerInvariant();

        var existingAdmin = await _context.PlatformUsers
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (existingAdmin is not null)
        {
            _logger.LogInformation("Platform admin user '{Email}' already exists. Skipping seeder.", normalizedEmail);
            return;
        }

        var adminRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Code == "admin", cancellationToken);

        if (adminRole is null)
        {
            _logger.LogError("Admin role 'admin' not found in database. Cannot seed admin user.");
            return;
        }

        var passwordHash = _passwordHasher.HashPassword(adminPassword);
        var userResult = PlatformUser.Create(normalizedEmail, passwordHash, adminFirstName, adminLastName, DateTime.UtcNow);

        if (userResult.IsFailure)
        {
            _logger.LogError("Failed to create admin user entity: {Error}", userResult.Error.Description);
            return;
        }

        var adminUser = userResult.Value;
        _context.PlatformUsers.Add(adminUser);
        _context.PlatformUserRoles.Add(new PlatformUserRole(adminUser.Id, adminRole.Id));

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully seeded default platform admin user: {Email}", normalizedEmail);
    }
}
