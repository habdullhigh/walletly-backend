using Walletly.Application.Abstractions.Notifications;
using Walletly.Infrastructure.Notifications.Templates;

namespace Walletly.Infrastructure.Tests.Notifications;

public sealed class EmailTemplateRendererTests
{
    private readonly EmailTemplateRenderer _renderer = new();

    [Fact]
    public void AllDeclaredTokens_AreSubstituted_IntoSubjectAndBody()
    {
        var result = _renderer.Render("otp-verification", new Dictionary<string, string>
        {
            ["otp_code"] = "482913",
            ["first_name"] = "Ada",
            ["expires_in_minutes"] = "10"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Your Walletly verification code is 482913", result.Value.Subject);
        Assert.Contains("482913", result.Value.HtmlContent);
        Assert.Contains("Hi Ada,", result.Value.HtmlContent);
        Assert.DoesNotContain("{{", result.Value.HtmlContent);
    }

    [Fact]
    public void TokenValues_AreHtmlEncoded_InBody()
    {
        var result = _renderer.Render("otp-verification", new Dictionary<string, string>
        {
            ["otp_code"] = "482913",
            ["first_name"] = "<script>alert(1)</script>",
            ["expires_in_minutes"] = "10"
        });

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("<script>alert(1)</script>", result.Value.HtmlContent);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", result.Value.HtmlContent);
    }

    [Fact]
    public void LineBreaks_InSubjectValues_AreRemoved()
    {
        var result = _renderer.Render("otp-verification", new Dictionary<string, string>
        {
            ["otp_code"] = "482913\r\nBcc: attacker@example.com",
            ["first_name"] = "Ada",
            ["expires_in_minutes"] = "10"
        });

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain('\r', result.Value.Subject);
        Assert.DoesNotContain('\n', result.Value.Subject);
    }

    [Fact]
    public void MissingToken_Fails_WithMissingTokenError()
    {
        var result = _renderer.Render("otp-verification", new Dictionary<string, string>
        {
            ["otp_code"] = "482913",
            ["first_name"] = "Ada"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(NotificationErrors.MissingToken, result.Error);
    }

    [Fact]
    public void UndeclaredToken_Fails_WithUnknownTokenError()
    {
        var result = _renderer.Render("otp-verification", new Dictionary<string, string>
        {
            ["otp_code"] = "482913",
            ["first_name"] = "Ada",
            ["expires_in_minutes"] = "10",
            ["admin_note"] = "x"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(NotificationErrors.UnknownToken, result.Error);
    }

    [Theory]
    [InlineData("missing-template")]
    [InlineData("../appsettings")]
    [InlineData("OTP-Verification")]
    public void UnknownOrInvalidTemplateKey_Fails_WithTemplateNotFound(string templateKey)
    {
        var result = _renderer.Render(templateKey, new Dictionary<string, string>());

        Assert.True(result.IsFailure);
        Assert.Equal(NotificationErrors.TemplateNotFound, result.Error);
    }

    [Fact]
    public void TenantVerificationTemplate_RendersSubjectAndBody_WithAllTokens()
    {
        var result = _renderer.Render("tenant-verification", new Dictionary<string, string>
        {
            ["first_name"] = "Alice",
            ["tenant_name"] = "Acme Corp",
            ["otp_code"] = "948201",
            ["expires_in_minutes"] = "15"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Verify your email for Acme Corp on Walletly", result.Value.Subject);
        Assert.Contains("Hi Alice,", result.Value.HtmlContent);
        Assert.Contains("Acme Corp", result.Value.HtmlContent);
        Assert.Contains("948201", result.Value.HtmlContent);
        Assert.Contains("15 minutes", result.Value.HtmlContent);
        Assert.DoesNotContain("{{", result.Value.HtmlContent);
    }

    [Fact]
    public void TenantWelcomeTemplate_RendersSubjectAndBody_WithAllTokens()
    {
        var result = _renderer.Render("tenant-welcome", new Dictionary<string, string>
        {
            ["first_name"] = "Alice",
            ["tenant_name"] = "Acme Corp",
            ["dashboard_url"] = "https://app.walletly.io/dashboard",
            ["support_email"] = "dev-support@walletly.io"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Welcome to Walletly, Acme Corp!", result.Value.Subject);
        Assert.Contains("Welcome aboard, Alice!", result.Value.HtmlContent);
        Assert.Contains("Acme Corp", result.Value.HtmlContent);
        Assert.Contains("https://app.walletly.io/dashboard", result.Value.HtmlContent);
        Assert.Contains("dev-support@walletly.io", result.Value.HtmlContent);
        Assert.DoesNotContain("{{", result.Value.HtmlContent);
    }

    [Fact]
    public void TenantInvitationTemplate_RendersSubjectAndBody_WithAllTokens()
    {
        var result = _renderer.Render("tenant-invitation", new Dictionary<string, string>
        {
            ["invitee_name"] = "Bob Smith",
            ["inviter_name"] = "Alice Johnson",
            ["tenant_name"] = "Acme Corp",
            ["role"] = "Developer",
            ["invite_url"] = "https://app.walletly.io/invites/abc-123",
            ["expires_in_days"] = "7"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Alice Johnson invited you to join Acme Corp on Walletly", result.Value.Subject);
        Assert.Contains("Hello Bob Smith,", result.Value.HtmlContent);
        Assert.Contains("Alice Johnson", result.Value.HtmlContent);
        Assert.Contains("Acme Corp", result.Value.HtmlContent);
        Assert.Contains("Developer", result.Value.HtmlContent);
        Assert.Contains("https://app.walletly.io/invites/abc-123", result.Value.HtmlContent);
        Assert.Contains("7 days", result.Value.HtmlContent);
        Assert.DoesNotContain("{{", result.Value.HtmlContent);
    }

    [Fact]
    public void TenantActivatedTemplate_RendersSubjectAndBody_WithAllTokens()
    {
        var result = _renderer.Render("tenant-activated", new Dictionary<string, string>
        {
            ["first_name"] = "Alice",
            ["tenant_name"] = "Acme Corp",
            ["dashboard_url"] = "https://app.walletly.io/dashboard"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Your Walletly account for Acme Corp is now active!", result.Value.Subject);
        Assert.Contains("Congratulations, Alice!", result.Value.HtmlContent);
        Assert.Contains("Acme Corp", result.Value.HtmlContent);
        Assert.Contains("https://app.walletly.io/dashboard", result.Value.HtmlContent);
        Assert.DoesNotContain("{{", result.Value.HtmlContent);
    }
}
