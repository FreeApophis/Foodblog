using System.Collections.Immutable;
using System.Security.Cryptography;

namespace FunctionalBlog.Identity;

public static class AuthHandlers
{
    private static readonly string DummyHash = new Pbkdf2PasswordHasher().Hash("timing-dummy");

    public static App NewRegisterForm => _ => env =>
        ValueTask.FromResult(Response.Html(
            AuthViews.RegisterForm([], string.Empty, string.Empty, env.Ctx)));

    public static App Register => request => async env =>
        await RegisterForm.Decode(request).Match(
            failure: f => Task.FromResult(Response.Html(
                AuthViews.RegisterForm(
                    f.Error,
                    request.Form.GetValueOrNone("email").GetOrElse(string.Empty),
                    request.Form.GetValueOrNone("displayName").GetOrElse(string.Empty),
                    env.Ctx),
                400)),
            success: async s =>
            {
                var existing = await env.Users.FindByEmail(s.Value.Email);

                // An already-verified account owns this email — refuse the registration.
                if (existing is [var found] && found.EmailVerified)
                {
                    return Response.Html(
                        AuthViews.RegisterForm(
                            ["auth.error.email_taken"],
                            s.Value.Email.Value,
                            s.Value.DisplayName.Value,
                            env.Ctx),
                        400);
                }

                // New email, or an unverified account that never confirmed: (re)create/keep the
                // account, then send a fresh verification link. No session — login is gated on
                // verification.
                User account;
                if (existing is [var unverified])
                {
                    account = unverified;
                }
                else
                {
                    var id = await env.Users.NextId();
                    var hash = env.PasswordHasher.Hash(s.Value.Password);
                    var roleNames = (await env.Roles.FindByName("Benutzer")).Select(role => role.Name).ToEnumerable().ToImmutableList();
                    account = User.Create(id, s.Value.Email, s.Value.DisplayName, hash, roleNames, env.Clock.Now);
                    await env.Users.Save(account);
                }

                await SendVerification(env, account);
                return Response.Html(AuthViews.VerificationPending(account.Email.Value, env.Ctx));
            });

    public static App NewLoginForm => _ => env =>
        ValueTask.FromResult(Response.Html(
            AuthViews.LoginForm([], string.Empty, env.Ctx)));

    public static App Login => request => async env =>
        await LoginForm.Decode(request).Match(
            failure: f => Task.FromResult(Response.Html(
                AuthViews.LoginForm(
                    f.Error,
                    request.Form.GetValueOrNone("email").GetOrElse(string.Empty),
                    env.Ctx),
                400)),
            success: async s =>
            {
                var userOption = await env.Users.FindByEmail(s.Value.Email);
                var hashToVerify = userOption.Match(none: () => DummyHash, some: u => u.PasswordHash);
                var passwordMatch = env.PasswordHasher.Verify(s.Value.Password, hashToVerify);

                if (userOption is not [var user] || !passwordMatch)
                {
                    return Response.Html(
                        AuthViews.LoginForm(
                            ["auth.error.invalid_credentials"],
                            s.Value.Email.Value,
                            env.Ctx),
                        401);
                }

                // Credentials are correct, but an unconfirmed account cannot log in yet.
                if (!user.EmailVerified)
                {
                    return Response.Html(AuthViews.VerificationPending(user.Email.Value, env.Ctx));
                }

                var session = NewSession(user.Id, env.Clock.Now);
                await env.Sessions.Save(session);
                return RedirectWithSession("/", session, env.Clock.Now);
            });

    public static App Logout => request => async env =>
    {
        if (request.Cookies.GetValueOrNone("session") is [var token])
        {
            await env.Sessions.Delete(token);
        }

        return Response.Redirect("/login").WithCookie(CookieHelper.ExpireSessionCookie());
    };

    public static App NewPasswordResetForm => _ => env =>
        ValueTask.FromResult(Response.Html(
            AuthViews.PasswordResetRequestForm(env.Ctx)));

    public static App RequestPasswordReset => request => async env =>
    {
        var decoded = PasswordResetRequestForm.Decode(request);

        var userOption = await Email.ParseOrNone(decoded.EmailRaw)
            .Match(
                none: () => Task.FromResult(Option<User>.None),
                some: async email => await env.Users.FindByEmail(email));
        await userOption.Match(
            none: () => Task.CompletedTask,
            some: async user =>
            {
                var token = GenerateToken();
                var resetToken = new PasswordResetToken(token, user.Id, env.Clock.Now.AddHours(1), Consumed: false);
                await env.PasswordResets.Save(resetToken);
                await TrySendWithLink(env, decoded.EmailRaw, "auth.email.reset_subject", "auth.email.reset_body", $"/password-reset/confirm?token={token}");
            });

        return Response.Html(AuthViews.PasswordResetRequested(env.Ctx));
    };

    // Landing endpoint for the emailed verification link: marks the account verified and consumes
    // the token. Invalid, already-used or expired tokens render a neutral failure page.
    public static App VerifyEmail => request => async env =>
    {
        var token = request.Query.GetValueOrDefault("token", string.Empty);
        var valid = (await env.EmailVerifications!.Find(token))
            .SelectMany(t => !t.Consumed && t.ExpiresAt > env.Clock.Now
                ? Option.Some(t)
                : Option<EmailVerificationToken>.None);

        return await valid.Match(
            none: () => Task.FromResult(Response.Html(AuthViews.VerifyResult(success: false, env.Ctx), 400)),
            some: async t =>
                await (await env.Users.FindById(t.UserId)).Match(
                    none: () => Task.FromResult(Response.Html(AuthViews.VerifyResult(success: false, env.Ctx), 400)),
                    some: async user =>
                    {
                        await env.Users.Save(user with { EmailVerified = true });
                        await env.EmailVerifications!.Consume(token);
                        return Response.Html(AuthViews.VerifyResult(success: true, env.Ctx));
                    }));
    };

    // Re-sends the verification link for an unverified account. Always renders the same neutral
    // pending page so the form never reveals whether an email is registered.
    public static App ResendVerification => request => async env =>
    {
        var emailRaw = request.Form.GetValueOrNone("email").GetOrElse(string.Empty);

        var userOption = await Email.ParseOrNone(emailRaw).Match(
            none: () => Task.FromResult(Option<User>.None),
            some: async email => await env.Users.FindByEmail(email));

        await userOption.Match(
            none: () => Task.CompletedTask,
            some: async user =>
            {
                if (!user.EmailVerified)
                {
                    await SendVerification(env, user);
                }
            });

        return Response.Html(AuthViews.VerificationPending(emailRaw, env.Ctx));
    };

    // Issues a fresh verification token (valid 24h) and emails the confirmation link.
    private static async Task SendVerification(Env env, User user)
    {
        if (env.EmailVerifications is null)
        {
            return;
        }

        var token = GenerateToken();
        await env.EmailVerifications.Save(new EmailVerificationToken(token, user.Id, env.Clock.Now.AddHours(24), Consumed: false));
        await TrySendWithLink(env, user.Email.Value, "auth.email.verify_subject", "auth.email.verify_body", $"/verify-email?token={token}");
    }

    // Sends a mail whose body ends with an absolute link built from the configured site.url. Without
    // a valid site.url the link would be relative and unusable, so nothing is sent and the problem
    // is logged (the admin dashboard also warns about it). The request's Host header is never used.
    private static async Task TrySendWithLink(Env env, string to, string subjectKey, string bodyKey, string pathAndQuery)
    {
        if (Option.FromNullable(env.Config).SelectMany(config => config.PublicBaseUrl) is not [var baseUrl])
        {
            env.Log.Error(new InvalidOperationException(
                $"E-Mail an {to} nicht gesendet: site.url ist nicht gesetzt oder keine absolute https://-URL. " +
                "Bitte unter /admin/settings die Basis-URL eintragen."));
            return;
        }

        await TrySend(env, to, env.T(subjectKey), env.T(bodyKey) + "\n\n" + baseUrl + pathAndQuery);
    }

    // Best-effort transactional send: a mail failure is logged, never surfaced to the user, so a
    // flaky SMTP server can't 500 a registration or password-reset request.
    private static async Task TrySend(Env env, string to, string subject, string body)
    {
        if (env.Email is null)
        {
            return;
        }

        try
        {
            await env.Email.Send(to, subject, body);
        }
        catch (Exception exception)
        {
            env.Log.Error(exception);
        }
    }

    public static App NewPasswordResetConfirmForm => request => env =>
    {
        var token = request.Query.GetValueOrDefault("token", string.Empty);
        return ValueTask.FromResult(Response.Html(
            AuthViews.PasswordResetConfirmForm([], token, env.Ctx)));
    };

    public static App ConfirmPasswordReset => request => async env =>
        await PasswordResetConfirmForm.Decode(request).Match(
            failure: f => Task.FromResult(Response.Html(
                AuthViews.PasswordResetConfirmForm(
                    f.Error,
                    request.Form.GetValueOrNone("token").GetOrElse(string.Empty),
                    env.Ctx),
                400)),
            success: async s =>
            {
                var tokenInvalid = Response.Html(
                    AuthViews.PasswordResetConfirmForm(["auth.error.reset_token_invalid"], string.Empty, env.Ctx),
                    400);

                var validToken = (await env.PasswordResets.Find(s.Value.Token))
                    .SelectMany(t => !t.Consumed && t.ExpiresAt > env.Clock.Now
                        ? Option.Some(t)
                        : Option<PasswordResetToken>.None);

                return await validToken.Match(
                    none: () => Task.FromResult(tokenInvalid),
                    some: async token =>
                        await (await env.Users.FindById(token.UserId)).Match(
                            none: () => Task.FromResult(Response.NotFound(env.Ctx)),
                            some: async user =>
                            {
                                await env.Users.Save(user with { PasswordHash = env.PasswordHasher.Hash(s.Value.Password) });
                                await env.PasswordResets.Consume(s.Value.Token);
                                return Response.Redirect("/login");
                            }));
            });

    private static Session NewSession(UserId userId, DateTimeOffset now) =>
        new(GenerateToken(), userId, now.AddDays(30));

    private static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static Response RedirectWithSession(string location, Session session, DateTimeOffset now) =>
        Response.Redirect(location)
            .WithCookie(CookieHelper.SessionCookie(session.Token, now.AddDays(30)));
}
