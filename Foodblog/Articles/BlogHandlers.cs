namespace Foodblog.Articles;

public static class BlogHandlers
{
    public static App Index => _ => async env =>
    {
        var articles = await env.Articles.All();
        var users = await env.Users.All();
        var recipes = await env.Recipes.All();
        var authorNames = users.ToDictionary(u => u.Id, u => u.DisplayName.Value);
        var authorAvatars = users.ToDictionary(u => u.Id, u => u.AvatarImageId);
        return Response.Html(BlogViews.Index(articles, authorNames, authorAvatars, recipes, env.Ctx));
    };

    public static App ShowArticle(ArticleId id) => request => async env =>
    {
        if ((await env.Articles.Find(id)) is [var article])
        {
            var author = await env.Users.FindById(article.AuthorId);
            var authorName = author.Select(u => u.DisplayName.Value).GetOrElse("?");
            var authorAvatar = author.SelectMany(u => u.AvatarImageId);

            return Response.Html(BlogViews.Show(article, authorName, env.Ctx, request.BaseUrl, authorAvatar));
        }

        return Response.NotFound(env.Ctx);
    };

    public static App NewArticleForm => _ => env =>
        ValueTask.FromResult(Response.Html(BlogViews.Form([], string.Empty, string.Empty, string.Empty, env.Ctx)));

    public static App CreateArticle => request => async env =>
        await DecodeWithCover(request).Match(
            failure: f => Task.FromResult(RenderForm(request, env, f.Error, Option<ImageId>.None)),
            success: async s =>
            {
                var article = Article.Create(
                    id: await env.Articles.NextId(),
                    title: s.Value.Form.Title,
                    teaser: s.Value.Form.Teaser,
                    text: s.Value.Form.Text,
                    authorId: ((AuthenticatedUser)env.CurrentUser).Id,
                    publishedAt: env.Clock.Now,
                    coverImageId: await ResolveCover(env, request, s.Value.Cover, Option<ImageId>.None));

                await env.Articles.Save(article);
                env.Search?.IndexArticle(article);

                var slug = await env.EnsureSlug(SlugEntityType.Article, article.Id.Value, article.Title.Value);
                return Response.Redirect($"/articles/{slug}");
            });

    public static App EditArticleForm(ArticleId id) => _ => async env =>
    {
        if ((await env.Articles.Find(id)) is [var article])
        {
            return Response.Html(BlogViews.Form(
                [],
                article.Title.Value,
                article.Teaser.Value,
                article.Text.Value,
                env.Ctx,
                formAction: $"/articles/{id.Value}",
                titleKey: "article.edit_title",
                currentCover: article.CoverImageId));
        }

        return Response.NotFound(env.Ctx);
    };

    public static App DeleteArticle(ArticleId id) => _ => async env =>
    {
        if ((await env.Articles.Find(id)) is [var article])
        {
            await env.Articles.Delete(id);
            env.Search?.DeleteDocument("article", article.Id.Value);
            return Response.Redirect("/");
        }

        return Response.NotFound(env.Ctx);
    };

    public static App UpdateArticle(ArticleId id) => request => async env =>
    {
        if ((await env.Articles.Find(id)) is not [var existing])
        {
            return Response.NotFound(env.Ctx);
        }

        return await DecodeWithCover(request).Match(
            failure: f => Task.FromResult(RenderForm(
                request,
                env,
                f.Error,
                existing.CoverImageId,
                formAction: $"/articles/{id.Value}",
                titleKey: "article.edit_title")),
            success: async s =>
            {
                var updated = Article.Create(
                    id: id,
                    title: s.Value.Form.Title,
                    teaser: s.Value.Form.Teaser,
                    text: s.Value.Form.Text,
                    authorId: existing.AuthorId,
                    publishedAt: existing.PublishedAt,
                    coverImageId: await ResolveCover(env, request, s.Value.Cover, existing.CoverImageId));

                await env.Articles.Save(updated);
                env.Search?.IndexArticle(updated);

                var slug = await env.EnsureSlug(SlugEntityType.Article, id.Value, updated.Title.Value);
                return Response.Redirect($"/articles/{slug}");
            });
    };

    private sealed record FormWithCover(ArticleForm.Valid Form, Option<ImageUploadForm.Valid> Cover);

    private static Validated<IReadOnlyList<string>, FormWithCover> DecodeWithCover(Request request)
    {
        Func<ArticleForm.Valid, Option<ImageUploadForm.Valid>, FormWithCover> combine =
            (form, cover) => new FormWithCover(form, cover);

        return combine
            .Apply(ArticleForm.Decode(request), CombineErrors)
            .Apply(ImageUploadForm.DecodeOptional(request, "cover"), CombineErrors);
    }

    private static async Task<Option<ImageId>> ResolveCover(
        Env env,
        Request request,
        Option<ImageUploadForm.Valid> upload,
        Option<ImageId> existing)
    {
        if (upload is [var newCover])
        {
            var image = Image.Create(
                id: await env.Images.NextId(),
                fileName: newCover.FileName,
                contentType: newCover.ContentType,
                data: newCover.Content,
                uploadedBy: ((AuthenticatedUser)env.CurrentUser).Id,
                createdAt: env.Clock.Now);

            await env.Images.Save(image);
            return Option.Some(image.Id);
        }

        var removeRequested = request.Form.GetValueOrNone("remove_cover").Match(none: false, some: v => v is "on" or "true");
        return removeRequested ? Option<ImageId>.None : existing;
    }

    private static Response RenderForm(
        Request request,
        Env env,
        IReadOnlyList<string> errors,
        Option<ImageId> currentCover,
        string formAction = "/articles",
        string titleKey = "article.new_title") =>
        Response.Html(
            BlogViews.Form(
                errors,
                request.Form.GetValueOrNone("title").GetOrElse(string.Empty),
                request.Form.GetValueOrNone("teaser").GetOrElse(string.Empty),
                request.Form.GetValueOrNone("text").GetOrElse(string.Empty),
                env.Ctx,
                formAction: formAction,
                titleKey: titleKey,
                currentCover: currentCover),
            400);

    private static IReadOnlyList<string> CombineErrors(IReadOnlyList<string> a, IReadOnlyList<string> b) => [..a, ..b];
}
