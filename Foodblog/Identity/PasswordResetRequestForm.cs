namespace Foodblog.Identity;

public static class PasswordResetRequestForm
{
    public static DecodedPasswordResetRequestForm Decode(Request request) =>
        new(request.Form.GetValueOrDefault("email", string.Empty).Trim().ToLowerInvariant());
}
