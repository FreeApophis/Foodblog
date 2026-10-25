using System.Collections.Concurrent;

namespace Foodblog;

public static class StaticHandlers
{
    private static readonly Lazy<string> StylesContent = new(LoadStyles);
    private static readonly Lazy<string> HtmxContent = new(LoadHtmx);
    private static readonly Lazy<string> ComboboxKeysContent = new(LoadComboboxKeys);
    private static readonly Lazy<string> ConfirmDeleteContent = new(LoadConfirmDelete);
    private static readonly Lazy<string> AutofocusSwapContent = new(LoadAutofocusSwap);
    private static readonly Lazy<string> FocalPointContent = new(LoadFocalPoint);
    private static readonly Lazy<string> SelectOnClickContent = new(LoadSelectOnClick);
    private static readonly ConcurrentDictionary<string, byte[]> BinaryCache = new();

    // Self-hosted design fonts (latin subset, variable weight). Whitelist of servable files.
    private static readonly IReadOnlyDictionary<string, string> Fonts = new Dictionary<string, string>
    {
        ["hanken-grotesk.woff2"] = "Foodblog.wwwroot.fonts.hanken-grotesk.woff2",
        ["jetbrains-mono.woff2"] = "Foodblog.wwwroot.fonts.jetbrains-mono.woff2",
        ["newsreader.woff2"] = "Foodblog.wwwroot.fonts.newsreader.woff2",
        ["newsreader-italic.woff2"] = "Foodblog.wwwroot.fonts.newsreader-italic.woff2",
    };

    // Static design assets (logos, banners). Whitelist of servable files.
    private static readonly IReadOnlyDictionary<string, string> Assets = new Dictionary<string, string>
    {
        ["foodblog-banner.png"] = "Foodblog.wwwroot.images.foodblog-banner.png",
    };

    private static readonly IReadOnlyDictionary<string, string> ImmutableHeaders =
        new Dictionary<string, string> { ["Cache-Control"] = "public, max-age=31536000, immutable" };

    public static App Styles => _ => _ =>
        ValueTask.FromResult(Response.Css(StylesContent.Value));

    public static App HtmxScript => _ => _ =>
        ValueTask.FromResult(Response.Js(HtmxContent.Value));

    public static App ComboboxKeysScript => _ => _ =>
        ValueTask.FromResult(Response.Js(ComboboxKeysContent.Value));

    public static App ConfirmDeleteScript => _ => _ =>
        ValueTask.FromResult(Response.Js(ConfirmDeleteContent.Value));

    public static App AutofocusSwapScript => _ => _ =>
        ValueTask.FromResult(Response.Js(AutofocusSwapContent.Value));

    public static App FocalPointScript => _ => _ =>
        ValueTask.FromResult(Response.Js(FocalPointContent.Value));

    public static App SelectOnClickScript => _ => _ =>
        ValueTask.FromResult(Response.Js(SelectOnClickContent.Value));

    public static App FaviconIco => _ => _ =>
        ValueTask.FromResult(Response.Bytes(
            "image/x-icon",
            BinaryCache.GetOrAdd("Foodblog.wwwroot.favicon.ico", LoadResourceBytes)));

    public static App FaviconPng => _ => _ =>
        ValueTask.FromResult(Response.Bytes(
            "image/png",
            BinaryCache.GetOrAdd("Foodblog.wwwroot.favicon.png", LoadResourceBytes)));

    public static App Font(string file) => _ => _ =>
        Fonts.GetValueOrNone(file) is [var resourceName]
            ? ValueTask.FromResult(Response.Bytes(
                "font/woff2",
                BinaryCache.GetOrAdd(resourceName, LoadResourceBytes),
                ImmutableHeaders))
            : ValueTask.FromResult(Response.NotFound());

    public static App Asset(string file) => _ => _ =>
        Assets.GetValueOrNone(file) is [var resourceName]
            ? ValueTask.FromResult(Response.Bytes(
                "image/png",
                BinaryCache.GetOrAdd(resourceName, LoadResourceBytes),
                ImmutableHeaders))
            : ValueTask.FromResult(Response.NotFound());

    private static string LoadStyles()
    {
        const string resourceName = "Foodblog.wwwroot.styles.css";
        return LoadResource(resourceName);
    }

    private static string LoadHtmx()
    {
        const string resourceName = "Foodblog.wwwroot.htmx.min.js";
        return LoadResource(resourceName);
    }

    private static string LoadComboboxKeys()
    {
        const string resourceName = "Foodblog.wwwroot.combobox-keys.js";
        return LoadResource(resourceName);
    }

    private static string LoadConfirmDelete()
    {
        const string resourceName = "Foodblog.wwwroot.confirm-delete.js";
        return LoadResource(resourceName);
    }

    private static string LoadAutofocusSwap()
    {
        const string resourceName = "Foodblog.wwwroot.autofocus-swap.js";
        return LoadResource(resourceName);
    }

    private static string LoadFocalPoint()
    {
        const string resourceName = "Foodblog.wwwroot.focal-point.js";
        return LoadResource(resourceName);
    }

    private static string LoadSelectOnClick()
    {
        const string resourceName = "Foodblog.wwwroot.select-on-click.js";
        return LoadResource(resourceName);
    }

    private static string LoadResource(string resourceName)
    {
        using var stream = typeof(StaticHandlers).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static byte[] LoadResourceBytes(string resourceName)
    {
        using var stream = typeof(StaticHandlers).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
