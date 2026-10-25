namespace Foodblog.Identity;

public static class UserSettingsHandlers
{
    public static App Settings => _ => env =>
    {
        var user = (AuthenticatedUser)env.CurrentUser;
        return ValueTask.FromResult(Response.Html(
            UserSettingsViews.Settings(user, [], [], env.Ctx)));
    };

    public static App UpdateAvatar => request => async env =>
    {
        var user = (AuthenticatedUser)env.CurrentUser;

        if ((await env.Users.FindById(user.Id)) is not [var stored])
        {
            return Response.NotFound(env.Ctx);
        }

        return await ImageUploadForm.DecodeOptional(request, "avatar").Match(
            failure: f => Task.FromResult(Response.Html(
                UserSettingsViews.Settings(user, [], f.Error, env.Ctx), 400)),
            success: async s =>
            {
                if (s.Value is [var newAvatar])
                {
                    var image = Image.Create(
                        id: await env.Images.NextId(),
                        fileName: newAvatar.FileName,
                        contentType: newAvatar.ContentType,
                        data: newAvatar.Content,
                        uploadedBy: stored.Id,
                        createdAt: env.Clock.Now);
                    await env.Images.Save(image);
                    await env.Users.Save(stored with { AvatarImageId = Option.Some(image.Id) });
                }
                else if (request.Form.GetValueOrNone("remove_avatar") is [var flag] && flag is "on" or "true")
                {
                    await env.Users.Save(stored with { AvatarImageId = Option<ImageId>.None });
                }

                return Response.Redirect("/settings");
            });
    };

    public static App ChangePassword => request => async env =>
    {
        var user = (AuthenticatedUser)env.CurrentUser;

        return await ChangePasswordForm.Decode(request).Match(
            failure: f => Task.FromResult(Response.Html(
                UserSettingsViews.Settings(user, f.Error, [], env.Ctx), 400)),
            success: async s =>
            {
                if ((await env.Users.FindById(user.Id)) is not [var stored] ||
                    !env.PasswordHasher.Verify(s.Value.CurrentPassword, stored.PasswordHash))
                {
                    return Response.Html(
                        UserSettingsViews.Settings(user, ["auth.error.current_password_wrong"], [], env.Ctx),
                        400);
                }

                await env.Users.Save(stored with { PasswordHash = env.PasswordHasher.Hash(s.Value.NewPassword) });
                return Response.Redirect("/settings");
            });
    };
}
