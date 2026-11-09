using System.Text.Json;
using Walletly.Application.Common;
using Walletly.Application.DTOs.Onboarding;
using Walletly.Application.Services;
using Walletly.Domain.Errors;

namespace Walletly.Infrastructure.Tests.Onboarding;

public sealed class RegisterTenantRequestTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Deserialization_BindsCamelCasePasswordConfirmation()
    {
        const string json = """
        {
            "tenantName": "Acme",
            "email": "ada@example.com",
            "password": "Password123!",
            "passwordConfirmation": "Password123!",
            "firstName": "Ada",
            "lastName": "Obi"
        }
        """;

        var request = JsonSerializer.Deserialize<RegisterTenantRequest>(json, WebOptions);

        Assert.NotNull(request);
        Assert.Equal("Password123!", request.Password);
        Assert.Equal("Password123!", request.PasswordConfirmation);
    }

    [Fact]
    public void PasswordConfirmation_DefaultsToEmpty()
    {
        var request = new RegisterTenantRequest();

        Assert.Equal(string.Empty, request.PasswordConfirmation);
    }

    [Fact]
    public async Task RegisterAsync_FailsWithWeakPassword_WhenPasswordIsTooShort()
    {
        var service = CreateService();
        var request = new RegisterTenantRequest
        {
            Password = "short",
            PasswordConfirmation = "short"
        };

        var result = await service.RegisterAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantUserErrors.WeakPassword, result.Error);
    }

    [Fact]
    public async Task RegisterAsync_FailsWithPasswordConfirmationRequired_WhenConfirmationIsEmpty()
    {
        var service = CreateService();
        var request = new RegisterTenantRequest
        {
            Password = "ValidPassword123!",
            PasswordConfirmation = string.Empty
        };

        var result = await service.RegisterAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantUserErrors.PasswordConfirmationRequired, result.Error);
    }

    [Fact]
    public async Task RegisterAsync_FailsWithPasswordMismatch_WhenPasswordsDoNotMatch()
    {
        var service = CreateService();
        var request = new RegisterTenantRequest
        {
            Password = "ValidPassword123!",
            PasswordConfirmation = "DifferentPassword123!"
        };

        var result = await service.RegisterAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantUserErrors.PasswordMismatch, result.Error);
    }

    private static TenantOnboardingService CreateService()
    {
        return new TenantOnboardingService(
            temporaryTenantRepository: null!,
            tenantRepository: null!,
            accountOwnerRepository: null!,
            unitOfWork: null!,
            passwordHasher: null!,
            webhookSecretProtector: null!,
            tenantTokenGenerator: null!,
            emailService: null!,
            dateTimeProvider: null!,
            options: new OnboardingOptions());
    }
}
