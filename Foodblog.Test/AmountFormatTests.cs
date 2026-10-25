namespace Foodblog.Test;

public sealed class AmountFormatTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(2, "2")]
    [InlineData(100, "100")]
    [InlineData(0.5, "½")]
    [InlineData(1.5, "1½")]
    [InlineData(2.5, "2½")]
    [InlineData(0.25, "¼")]
    [InlineData(0.75, "¾")]
    [InlineData(1.25, "1¼")]
    [InlineData(0.3333, "⅓")]
    [InlineData(0.6667, "⅔")]
    [InlineData(1.3333, "1⅓")]
    [InlineData(0.1667, "⅙")]
    [InlineData(0.8333, "⅚")]
    [InlineData(0.125, "⅛")]
    [InlineData(0.375, "⅜")]
    [InlineData(0.625, "⅝")]
    [InlineData(0.875, "⅞")]
    public void Format_snaps_near_fractions_to_glyphs(double amount, string expected)
    {
        Assert.Equal(expected, AmountFormat.Format((decimal)amount));
    }

    [Theory]
    [InlineData(0.51, "½")] // close enough to snap
    [InlineData(1.99, "2")] // carries to the next whole
    [InlineData(2.01, "2")] // drops the negligible remainder
    public void Format_tolerates_small_deviations(double amount, string expected)
    {
        Assert.Equal(expected, AmountFormat.Format((decimal)amount));
    }

    [Theory]
    [InlineData(50, "50")] // ≤ 50 keeps exact whole
    [InlineData(52, "50")] // > 50 → nearest 5
    [InlineData(77, "75")]
    [InlineData(88.3, "90")]
    [InlineData(100, "100")] // > 50 but ≤ 100 → nearest 5
    [InlineData(153.7, "150")] // > 100 → nearest 10
    [InlineData(162, "160")]
    [InlineData(166.5, "170")]
    [InlineData(248, "250")]
    public void Format_rounds_large_amounts(double amount, string expected)
    {
        Assert.Equal(expected, AmountFormat.Format((decimal)amount));
    }

    [Theory]
    [InlineData(0.4, "0.4")] // 2/5 — not a supported fraction
    [InlineData(0.1, "0.1")] // not close enough to 1/8
    [InlineData(0.7, "0.7")] // between 2/3 and 3/4
    public void Format_falls_back_to_decimals_for_unsupported_values(double amount, string expected)
    {
        Assert.Equal(expected, AmountFormat.Format((decimal)amount));
    }
}
