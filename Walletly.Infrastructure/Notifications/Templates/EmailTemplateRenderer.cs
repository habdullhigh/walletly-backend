using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using SharedKernel;
using Walletly.Application.Abstractions.Notifications;

namespace Walletly.Infrastructure.Notifications.Templates;

public interface IEmailTemplateRenderer
{
    Result<RenderedTemplate> Render(string templateKey, IReadOnlyDictionary<string, string> tokens);
}

public sealed record RenderedTemplate(string Subject, string HtmlContent);

// Templates are embedded resources, so they ship inside the assembly and cannot be changed at runtime.
// Tokens are declared by the template itself: {{token_name}} in the subject or the HTML body.
public sealed partial class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly Assembly ResourceAssembly = typeof(EmailTemplateRenderer).Assembly;

    public Result<RenderedTemplate> Render(string templateKey, IReadOnlyDictionary<string, string> tokens)
    {
        if (!TemplateKeyPattern().IsMatch(templateKey))
        {
            return NotificationErrors.TemplateNotFound;
        }

        var html = ReadResource($"Templates/{templateKey}.html");
        var subject = ReadResource($"Templates/{templateKey}.subject.txt");
        if (html is null || subject is null)
        {
            return NotificationErrors.TemplateNotFound;
        }

        var declared = TokenPattern().Matches(subject + html)
            .Select(match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

        if (tokens.Keys.Any(key => !declared.Contains(key)))
        {
            return NotificationErrors.UnknownToken;
        }

        if (declared.Any(name => !tokens.ContainsKey(name)))
        {
            return NotificationErrors.MissingToken;
        }

        // Values are HTML-encoded in the body. The subject is plain text, so only line breaks are removed
        // to prevent header injection.
        var renderedHtml = Substitute(html, name => WebUtility.HtmlEncode(tokens[name]));
        var renderedSubject = Substitute(subject, name => tokens[name].ReplaceLineEndings(" ")).Trim();

        return new RenderedTemplate(renderedSubject, renderedHtml);
    }

    private static string Substitute(string template, Func<string, string> valueFor) =>
        TokenPattern().Replace(template, match => valueFor(match.Groups["name"].Value));

    private static string? ReadResource(string resourceName)
    {
        using var stream = ResourceAssembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex TemplateKeyPattern();

    [GeneratedRegex(@"\{\{\s*(?<name>[a-z][a-z0-9_]*)\s*\}\}")]
    private static partial Regex TokenPattern();
}
