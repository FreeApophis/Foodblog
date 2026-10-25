namespace Foodblog.Pages;

public static class PageHandlers
{
    public static App Index => _ => async env =>
        Response.Html(PageViews.Index(await env.Pages.All(), env.Ctx));

    public static App ShowPage(PageId id) => request => async env =>
    {
        if ((await env.Pages.Find(id)) is [var page])
        {
            return Response.Html(PageViews.Show(page, env.Ctx, request.BaseUrl));
        }

        return Response.NotFound(env.Ctx);
    };

    public static App NewPageForm => _ => env =>
        ValueTask.FromResult(Response.Html(PageViews.Form([], string.Empty, string.Empty, env.Ctx)));

    public static App CreatePage => request => async env =>
        await PageForm.Decode(request).Match(
            failure: f => Task.FromResult(Response.Html(
                PageViews.Form(f.Error, ReadTitle(request), ReadContent(request), env.Ctx),
                400)),
            success: async s =>
            {
                var page = Page.Create(await env.Pages.NextId(), s.Value.Title, s.Value.Content);
                await env.Pages.Save(page);
                env.Search?.IndexPage(page);
                var slug = await env.EnsureSlug(SlugEntityType.Page, page.Id.Value, page.Title.Value);
                return Response.Redirect($"/pages/{slug}");
            });

    public static App EditPageForm(PageId id) => _ => async env =>
    {
        if ((await env.Pages.Find(id)) is [var page])
        {
            return Response.Html(PageViews.Form(
                [],
                page.Title.Value,
                page.Content.Value,
                env.Ctx,
                formAction: $"/pages/{id.Value}",
                titleKey: "page.edit_title"));
        }

        return Response.NotFound(env.Ctx);
    };

    public static App UpdatePage(PageId id) => request => async env =>
    {
        if ((await env.Pages.Find(id)) is not [_])
        {
            return Response.NotFound(env.Ctx);
        }

        return await PageForm.Decode(request).Match(
            failure: f => Task.FromResult(Response.Html(
                PageViews.Form(
                    f.Error,
                    ReadTitle(request),
                    ReadContent(request),
                    env.Ctx,
                    formAction: $"/pages/{id.Value}",
                    titleKey: "page.edit_title"),
                400)),
            success: async s =>
            {
                var updated = Page.Create(id, s.Value.Title, s.Value.Content);
                await env.Pages.Save(updated);
                env.Search?.IndexPage(updated);
                var slug = await env.EnsureSlug(SlugEntityType.Page, id.Value, updated.Title.Value);
                return Response.Redirect($"/pages/{slug}");
            });
    };

    public static App DeletePage(PageId id) => _ => async env =>
    {
        if ((await env.Pages.Find(id)) is [_])
        {
            await env.Pages.Delete(id);
            env.Search?.DeleteDocument("page", id.Value);
            return Response.Redirect("/pages");
        }

        return Response.NotFound(env.Ctx);
    };

    private static string ReadTitle(Request r) => r.Form.GetValueOrNone("title").GetOrElse(string.Empty);

    private static string ReadContent(Request r) => r.Form.GetValueOrNone("content").GetOrElse(string.Empty);
}
