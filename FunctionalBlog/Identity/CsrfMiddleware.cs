using System.Security.Cryptography;

namespace FunctionalBlog.Identity;

public static class CsrfMiddleware
{
    private const string TokenName = "_csrf";

    public static Middleware Create() => next => request => async env =>
    {
        var (token, isNewToken) = GetOrCreateToken(request);

        if (request.Method == HttpMethod.Post)
        {
            var formToken = request.Form.GetValueOrNone(TokenName).GetOrElse(string.Empty);
            if (string.IsNullOrEmpty(token) || formToken != token)
            {
                return Response.Forbidden(env.Ctx);
            }
        }

        var response = await next(request)(env with { CsrfToken = token });

        return isNewToken
            ? response.WithCookie(new Cookie(TokenName, token, CookieSameSite.Strict, HttpOnly: true))
            : response;
    };

    private static (string Token, bool IsNew) GetOrCreateToken(Request request)
    {
        if (request.Cookies.TryGetValue(TokenName, out var existing) && !string.IsNullOrEmpty(existing))
        {
            return (existing, false);
        }

        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (token, true);
    }
}
