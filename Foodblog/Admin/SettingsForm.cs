using System.Globalization;

namespace Foodblog.Admin;

// Decodes the admin settings form. The site name is required and the SMTP port and (optional)
// sender address are validated; everything else is free text. All field errors accumulate.
public static class SettingsForm
{
    public sealed record Valid(
        string SiteName,
        string SiteUrl,
        string SiteSince,
        string InstagramHandle,
        string SmtpHost,
        int SmtpPort,
        string SmtpUsername,
        string SmtpPassword,
        string SmtpFromAddress,
        string SmtpFromName,
        bool SmtpUseSsl);

    public static Validated<IReadOnlyList<string>, Valid> Decode(Request request)
    {
        var siteName = Field(request, "site_name");
        var siteUrl = Field(request, "site_url");
        var siteSince = Field(request, "site_since");
        var instagramHandle = Field(request, "instagram_handle");
        var smtpHost = Field(request, "smtp_host");
        var smtpPort = Field(request, "smtp_port");
        var smtpUsername = Field(request, "smtp_username");

        // The password is never trimmed and may legitimately be blank (meaning "keep the current one").
        var smtpPassword = request.Form.GetValueOrNone("smtp_password").GetOrElse(string.Empty);
        var smtpFromAddress = Field(request, "smtp_from_address");
        var smtpFromName = Field(request, "smtp_from_name");
        var smtpUseSsl = request.Form.GetValueOrNone("smtp_use_ssl") is [_];

        Func<string, string, string, string, string, int, string, string, string, string, bool, Valid> create =
            (name, url, since, instagram, host, port, user, pass, from, fromName, ssl) =>
                new Valid(name, url, since, instagram, host, port, user, pass, from, fromName, ssl);

        return create
            .Apply(TryParseSiteName(siteName), Combine)
            .Apply(TryParseSiteUrl(siteUrl), Combine)
            .Apply(TryParseSiteSince(siteSince), Combine)
            .Apply(TryParseInstagramHandle(instagramHandle), Combine)
            .Apply(Ok(smtpHost), Combine)
            .Apply(TryParsePort(smtpPort), Combine)
            .Apply(Ok(smtpUsername), Combine)
            .Apply(Ok(smtpPassword), Combine)
            .Apply(TryParseFromAddress(smtpFromAddress), Combine)
            .Apply(Ok(smtpFromName), Combine)
            .Apply(Ok(smtpUseSsl), Combine);
    }

    private static Validated<IReadOnlyList<string>, string> TryParseSiteName(string raw) =>
        raw.Length >= 1
            ? Ok(raw)
            : Validated.Fail<IReadOnlyList<string>, string>(["settings.error.site_name_required"]);

    // Optional, but when given it must be an absolute https:// URL (it becomes the base of email links).
    private static Validated<IReadOnlyList<string>, string> TryParseSiteUrl(string raw) =>
        raw.Length == 0
            ? Ok(raw)
            : SiteUrl.ParseOrNone(raw).Match(
                none: () => Validated.Fail<IReadOnlyList<string>, string>(["settings.error.site_url_invalid"]),
                some: Ok);

    // Optional; when given it must be a four-digit year.
    private static Validated<IReadOnlyList<string>, string> TryParseSiteSince(string raw) =>
        raw.Length == 0 || SiteSince.ParseOrNone(raw) is [_]
            ? Ok(raw)
            : Validated.Fail<IReadOnlyList<string>, string>(["settings.error.since_invalid"]);

    // Optional; when given it must be a plain handle. A leading "@" is dropped.
    private static Validated<IReadOnlyList<string>, string> TryParseInstagramHandle(string raw) =>
        raw.Length == 0
            ? Ok(raw)
            : InstagramHandle.ParseOrNone(raw).Match(
                none: () => Validated.Fail<IReadOnlyList<string>, string>(["settings.error.instagram_invalid"]),
                some: Ok);

    private static Validated<IReadOnlyList<string>, int> TryParsePort(string raw) =>
        int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value is >= 1 and <= 65535
            ? Ok(value)
            : Validated.Fail<IReadOnlyList<string>, int>(["settings.error.port_invalid"]);

    private static Validated<IReadOnlyList<string>, string> TryParseFromAddress(string raw) =>
        raw.Length == 0 || Email.ParseOrNone(raw) is [var _]
            ? Ok(raw)
            : Validated.Fail<IReadOnlyList<string>, string>(["settings.error.from_address_invalid"]);

    private static Validated<IReadOnlyList<string>, T> Ok<T>(T value) =>
        Validated.Succeed<IReadOnlyList<string>, T>(value);

    private static string Field(Request request, string name) =>
        request.Form.GetValueOrNone(name).GetOrElse(string.Empty).Trim();

    private static IReadOnlyList<string> Combine(IReadOnlyList<string> a, IReadOnlyList<string> b) => [.. a, .. b];
}
