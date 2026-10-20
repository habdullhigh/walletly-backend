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
}
