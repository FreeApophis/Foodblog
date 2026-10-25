namespace Foodblog;

public readonly struct HttpMethod : IEquatable<HttpMethod>
{
    private HttpMethod(string value) => Value = value;

    public string Value { get; }

    public static HttpMethod Get { get; } = new("GET");

    public static HttpMethod Post { get; } = new("POST");

    public static HttpMethod Parse(string value) => new(value.ToUpperInvariant());

    public bool Equals(HttpMethod other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is HttpMethod other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

    public static bool operator ==(HttpMethod left, HttpMethod right) => left.Equals(right);

    public static bool operator !=(HttpMethod left, HttpMethod right) => !left.Equals(right);

    public override string ToString() => Value;
}
