using System.Globalization;

namespace Foodblog.Admin;

public static class AdminSettingsHandlers
{
    public static App Show => _ => async env =>
        Response.Html(AdminSettingsViews.Page(await CurrentValues(env), [], saved: false, env.Ctx));

    public static App Save => request => async env =>
        await SettingsForm.Decode(request).Match(
            failure: f => Task.FromResult(Response.Html(
                AdminSettingsViews.Page(FormValues(request), f.Error, saved: false, env.Ctx),
                400)),
            success: async s =>
            {
                await Apply(env, s.Value);
                if (env.Config is not null)
                {
                    await env.Config.RefreshAsync();
                }

                return Response.Html(AdminSettingsViews.Page(await CurrentValues(env), [], saved: true, env.Ctx));
            });

    // Sends a one-off test message to confirm the SMTP settings work. Returns an htmx fragment
    // (success or the actual error) so the admin gets immediate feedback without leaving the page.
    public static App TestEmail => request => async env =>
    {
        var to = request.Form.GetValueOrNone("test_email").GetOrElse(string.Empty).Trim();
        if (to.Length == 0 && env.CurrentUser is AuthenticatedUser user)
        {
            to = user.Email.Value;
        }

        if (Email.ParseOrNone(to) is not [var _])
        {
            return Response.Html(AdminSettingsViews.TestResult(false, env.T("admin.settings.test_invalid"), env.Ctx), 400);
        }

        try
        {
            if (env.Email is null)
            {
                throw new InvalidOperationException(env.T("admin.settings.test_unavailable"));
            }

            await env.Email.Send(to, env.T("admin.settings.test_subject"), env.T("admin.settings.test_body"));
            return Response.Html(AdminSettingsViews.TestResult(true, env.T("admin.settings.test_sent"), env.Ctx));
        }
        catch (Exception exception)
        {
            env.Log.Error(exception);
            return Response.Html(AdminSettingsViews.TestResult(false, exception.Message, env.Ctx));
        }
    };

    private static async Task Apply(Env env, SettingsForm.Valid v)
    {
        if (env.Configuration is null)
        {
            return;
        }

        await env.Configuration.Set(ConfigurationKeys.SiteName, v.SiteName);
        await env.Configuration.Set(ConfigurationKeys.SiteUrl, v.SiteUrl);
        await env.Configuration.Set(ConfigurationKeys.SmtpHost, v.SmtpHost);
        await env.Configuration.Set(ConfigurationKeys.SmtpPort, v.SmtpPort.ToString(CultureInfo.InvariantCulture));
        await env.Configuration.Set(ConfigurationKeys.SmtpUsername, v.SmtpUsername);

        // A blank password means "keep the current one", so the stored secret is never wiped by a
        // form that (for security) never echoes it back.
        if (!string.IsNullOrEmpty(v.SmtpPassword))
        {
            await env.Configuration.Set(ConfigurationKeys.SmtpPassword, v.SmtpPassword);
        }

        await env.Configuration.Set(ConfigurationKeys.SmtpFromAddress, v.SmtpFromAddress);
        await env.Configuration.Set(ConfigurationKeys.SmtpFromName, v.SmtpFromName);
        await env.Configuration.Set(ConfigurationKeys.SmtpUseSsl, v.SmtpUseSsl ? "true" : "false");
    }

    // Current stored values, with defaults filled in for any key not yet persisted.
    private static async Task<IReadOnlyDictionary<string, string>> CurrentValues(Env env)
    {
        var values = new Dictionary<string, string>(ConfigurationDefaults.Values);
        if (env.Configuration is not null)
        {
            foreach (var (key, value) in await env.Configuration.All())
            {
                values[key] = value;
            }
        }

        return values;
    }

    // The submitted values, re-keyed to configuration keys, so a failed save re-renders the form
    // with what the admin typed (the password field is intentionally not echoed back).
    private static IReadOnlyDictionary<string, string> FormValues(Request request)
    {
        string F(string name) => request.Form.GetValueOrNone(name).GetOrElse(string.Empty);

        return new Dictionary<string, string>
        {
            [ConfigurationKeys.SiteName] = F("site_name"),
            [ConfigurationKeys.SiteUrl] = F("site_url"),
            [ConfigurationKeys.SmtpHost] = F("smtp_host"),
            [ConfigurationKeys.SmtpPort] = F("smtp_port"),
            [ConfigurationKeys.SmtpUsername] = F("smtp_username"),
            [ConfigurationKeys.SmtpFromAddress] = F("smtp_from_address"),
            [ConfigurationKeys.SmtpFromName] = F("smtp_from_name"),
            [ConfigurationKeys.SmtpUseSsl] = request.Form.GetValueOrNone("smtp_use_ssl") is [_] ? "true" : "false",
        };
    }
}
