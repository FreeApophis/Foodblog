using System.Net;

namespace Foodblog;

[DiscriminatedUnion]
public abstract partial record HtmlString
{
    public sealed partial record Safe(string Value) : HtmlString;

    public sealed partial record Encoded(string Value) : HtmlString;

    public static HtmlString operator +(HtmlString left, HtmlString right) =>
        new Safe(left.Render() + right.Render());

    public string Render()
        => Match(
            safe: safe => safe.Value,
            encoded: encoded => WebUtility.HtmlEncode(encoded.Value));

    public sealed override string ToString() => Render();

    public static readonly HtmlString Empty = new Safe(string.Empty);

    public static HtmlString Concat(IEnumerable<HtmlString> items) =>
        items.Aggregate(Empty, (acc, item) => acc + item);

    public static HtmlString Join(string separator, IEnumerable<HtmlString> items) =>
        new Safe(string.Join(WebUtility.HtmlEncode(separator), items.Select(i => i.Render())));
}
