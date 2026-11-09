namespace Walletly.Application.Common;

public sealed class OnboardingOptions
{
    public const string SectionName = "Onboarding";

    // Base URL of the tenant dashboard. Invitation and dashboard links in emails are built from it.
    public string PublicBaseUrl { get; set; } = "http://localhost:3000";

    public string SupportEmail { get; set; } = string.Empty;
}
