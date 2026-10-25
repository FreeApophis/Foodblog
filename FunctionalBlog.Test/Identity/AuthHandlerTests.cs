namespace FunctionalBlog.Test.Identity;

public sealed class AuthHandlerTests
{
    [Fact]
    public async Task Register_with_new_email_sends_verification_and_does_not_log_in()
    {
        var email = new RecordingEmailSender();
        var env = BuildEnv(email: email);
        await SeedRoles(env);

        var response = await AuthHandlers.Register(RegisterRequest("neu@blog.de", "geheim123", "geheim123"))(env);

        // No session — login is gated on verification — and a confirmation mail went out.
        Assert.Equal(200, response.Status);
        Assert.Empty(response.SetCookies);
        Assert.Single(email.Sent);
    }

    [Fact]
    public async Task Register_creates_an_unverified_account()
    {
        var env = BuildEnv();
        await SeedRoles(env);

        await AuthHandlers.Register(RegisterRequest("neu@blog.de", "geheim123", "geheim123"))(env);

        var user = FunctionalAssert.Some(await env.Users.FindByEmail(new Email("neu@blog.de")));
        Assert.False(user!.EmailVerified);
    }

    [Fact]
    public async Task Register_with_an_already_verified_email_is_rejected()
    {
        var email = new RecordingEmailSender();
        var env = BuildEnv(email: email);
        await SeedRoles(env);
        await AuthHandlers.Register(RegisterRequest("exist@blog.de", "geheim123", "geheim123"))(env);
        await AuthHandlers.VerifyEmail(VerifyRequest(email.ExtractToken()))(env);

        var response = await AuthHandlers.Register(RegisterRequest("exist@blog.de", "geheim123", "geheim123"))(env);

        Assert.Equal(400, response.Status);
    }

    [Fact]
    public async Task Register_again_with_an_unverified_email_resends_verification()
    {
        var env = BuildEnv();
        await SeedRoles(env);
        await AuthHandlers.Register(RegisterRequest("exist@blog.de", "geheim123", "geheim123"))(env);

        var response = await AuthHandlers.Register(RegisterRequest("exist@blog.de", "geheim123", "geheim123"))(env);

        // Pending page, not a leak of the account's existence via a different status.
        Assert.Equal(200, response.Status);
    }

    [Fact]
    public async Task Register_with_invalid_form_returns_400()
    {
        var env = BuildEnv();
        await SeedRoles(env);

        var response = await AuthHandlers.Register(RegisterRequest("notanemail", "kurz", "kurz"))(env);

        Assert.Equal(400, response.Status);
    }

    [Fact]
    public async Task Verifying_the_emailed_token_lets_the_user_log_in()
    {
        var email = new RecordingEmailSender();
        var env = BuildEnv(email: email);
        await SeedRoles(env);
        await AuthHandlers.Register(RegisterRequest("login@blog.de", "geheim123", "geheim123"))(env);

        var verify = await AuthHandlers.VerifyEmail(VerifyRequest(email.ExtractToken()))(env);
        var login = await AuthHandlers.Login(LoginRequest("login@blog.de", "geheim123"))(env);

        Assert.Equal(200, verify.Status);
        Assert.Equal(303, login.Status);
        Assert.Equal("/", login.Headers["Location"]);
        Assert.NotEmpty(login.SetCookies);
        Assert.All(login.SetCookies, c => Assert.EndsWith("; Secure", c));
    }

    [Fact]
    public async Task Login_before_verification_is_refused_without_a_session()
    {
        var env = BuildEnv();
        await SeedRoles(env);
        await AuthHandlers.Register(RegisterRequest("pending@blog.de", "geheim123", "geheim123"))(env);

        var response = await AuthHandlers.Login(LoginRequest("pending@blog.de", "geheim123"))(env);

        Assert.Equal(200, response.Status);
        Assert.Empty(response.SetCookies);
    }

    [Fact]
    public async Task Verify_with_an_unknown_token_returns_400()
    {
        var env = BuildEnv();

        var response = await AuthHandlers.VerifyEmail(VerifyRequest("does-not-exist"))(env);

        Assert.Equal(400, response.Status);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_same_status_as_unknown_email()
    {
        var env = BuildEnv();
        await SeedRoles(env);
        await AuthHandlers.Register(RegisterRequest("known@blog.de", "geheim123", "geheim123"))(env);

        var wrongPassword = await AuthHandlers.Login(LoginRequest("known@blog.de", "falsch"))(env);
        var unknownEmail = await AuthHandlers.Login(LoginRequest("unknown@blog.de", "geheim123"))(env);

        Assert.Equal(wrongPassword.Status, unknownEmail.Status);
    }

    [Fact]
    public async Task Logout_deletes_session_and_returns_expired_cookie()
    {
        var email = new RecordingEmailSender();
        var env = BuildEnv(email: email);
        await SeedRoles(env);
        await AuthHandlers.Register(RegisterRequest("logout@blog.de", "geheim123", "geheim123"))(env);
        await AuthHandlers.VerifyEmail(VerifyRequest(email.ExtractToken()))(env);
        var loginResponse = await AuthHandlers.Login(LoginRequest("logout@blog.de", "geheim123"))(env);
        var token = ExtractSessionToken(loginResponse);
        var authEnv = env with { CurrentUser = await AuthMiddleware.ResolvePrincipal(RequestWithCookie("session", token), env) };

        var response = await AuthHandlers.Logout(RequestWithCookie("session", token))(authEnv);

        Assert.Equal(303, response.Status);
        Assert.NotEmpty(response.SetCookies);
        Assert.Contains("Max-Age=0", response.SetCookies[0]);
        Assert.All(response.SetCookies, c => Assert.EndsWith("; Secure", c));
        FunctionalAssert.None(await env.Sessions.Find(token));
    }

    [Fact]
    public async Task Password_reset_request_always_returns_200()
    {
        var env = BuildEnv();

        var knownResponse = await AuthHandlers.RequestPasswordReset(ResetRequestRequest("known@blog.de"))(env);
        var unknownResponse = await AuthHandlers.RequestPasswordReset(ResetRequestRequest("unknown@blog.de"))(env);

        Assert.Equal(200, knownResponse.Status);
        Assert.Equal(200, unknownResponse.Status);
    }

    [Fact]
    public async Task Password_reset_request_emails_a_reset_link_to_a_known_user()
    {
        var email = new RecordingEmailSender();
        var env = BuildEnv(email: email);
        await SeedRoles(env);
        await AuthHandlers.Register(RegisterRequest("reset@blog.de", "altesPasswort1", "altesPasswort1"))(env);
        email.Sent.Clear();

        await AuthHandlers.RequestPasswordReset(ResetRequestRequest("reset@blog.de"))(env);

        Assert.Single(email.Sent);
        Assert.Contains("/password-reset/confirm?token=", email.Sent[0].Body);
    }

    [Fact]
    public async Task Confirm_reset_with_valid_token_updates_password_and_redirects_to_login()
    {
        var email = new RecordingEmailSender();
        var env = BuildEnv(email: email);
        await SeedRoles(env);
        await AuthHandlers.Register(RegisterRequest("reset@blog.de", "altesPasswort1", "altesPasswort1"))(env);
        await AuthHandlers.VerifyEmail(VerifyRequest(email.ExtractToken()))(env);
        await AuthHandlers.RequestPasswordReset(ResetRequestRequest("reset@blog.de"))(env);
        var token = email.ExtractToken();

        var response = await AuthHandlers.ConfirmPasswordReset(
            ResetConfirmRequest(token, "neuesPasswort1", "neuesPasswort1"))(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);

        var loginAfterReset = await AuthHandlers.Login(LoginRequest("reset@blog.de", "neuesPasswort1"))(env);
        Assert.Equal(303, loginAfterReset.Status);
    }

    [Fact]
    public async Task Confirm_reset_with_consumed_token_returns_400()
    {
        var email = new RecordingEmailSender();
        var env = BuildEnv(email: email);
        await SeedRoles(env);
        await AuthHandlers.Register(RegisterRequest("reset2@blog.de", "altesPasswort1", "altesPasswort1"))(env);
        await AuthHandlers.VerifyEmail(VerifyRequest(email.ExtractToken()))(env);
        await AuthHandlers.RequestPasswordReset(ResetRequestRequest("reset2@blog.de"))(env);
        var token = email.ExtractToken();
        await AuthHandlers.ConfirmPasswordReset(ResetConfirmRequest(token, "neues1234", "neues1234"))(env);

        var response = await AuthHandlers.ConfirmPasswordReset(
            ResetConfirmRequest(token, "nochanderes", "nochanderes"))(env);

        Assert.Equal(400, response.Status);
    }

    private static Env BuildEnv(ILog? log = null, IEmailSender? email = null) => new(
        Articles: new InMemoryArticleRepository(),
        Users: new InMemoryUserRepository(),
        Roles: new InMemoryRoleRepository(),
        Sessions: new InMemorySessionStore(),
        PasswordResets: new InMemoryPasswordResetTokenStore(),
        PasswordHasher: new Pbkdf2PasswordHasher(),
        Clock: new SystemClock(),
        Log: log ?? new ConsoleLog(),
        CurrentUser: Guest.Instance,
        Recipes: new InMemoryRecipeRepository(),
        Ingredients: new InMemoryIngredientRepository(),
        Units: new InMemoryUnitRepository(),
        Images: new InMemoryImageRepository(),
        Pages: new InMemoryPageRepository(),
        Email: email,
        EmailVerifications: new InMemoryEmailVerificationTokenStore());

    private static async Task SeedRoles(Env env)
    {
        var id = await env.Roles.NextId();
        await env.Roles.Save(Role.Create(id, "Benutzer"));
    }

    private static string ExtractSessionToken(Response response) =>
        response.SetCookies[0].Split(';')[0].Replace("session=", string.Empty);

    private static Request RequestWithCookie(string name, string value) =>
        new(HttpMethod.Get, "/", Empty, Empty, Empty, new Dictionary<string, string> { [name] = value });

    private static Request RegisterRequest(string email, string password, string confirmation) =>
        new(
            HttpMethod.Post,
            "/register",
            Empty,
            Empty,
            new Dictionary<string, string> { ["email"] = email, ["displayName"] = "Testbenutzer", ["password"] = password, ["confirmation"] = confirmation },
            Empty);

    private static Request LoginRequest(string email, string password) =>
        new(
            HttpMethod.Post,
            "/login",
            Empty,
            Empty,
            new Dictionary<string, string> { ["email"] = email, ["password"] = password },
            Empty);

    private static Request VerifyRequest(string token) =>
        new(
            HttpMethod.Get,
            "/verify-email",
            Empty,
            new Dictionary<string, string> { ["token"] = token },
            Empty,
            Empty);

    private static Request ResetRequestRequest(string email) =>
        new(
            HttpMethod.Post,
            "/password-reset",
            Empty,
            Empty,
            new Dictionary<string, string> { ["email"] = email },
            Empty);

    private static Request ResetConfirmRequest(string token, string password, string confirmation) =>
        new(
            HttpMethod.Post,
            "/password-reset/confirm",
            Empty,
            Empty,
            new Dictionary<string, string> { ["token"] = token, ["password"] = password, ["confirmation"] = confirmation },
            Empty);

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
