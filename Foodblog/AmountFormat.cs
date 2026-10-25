using System.Globalization;

namespace Foodblog;

// Formats a (possibly scaled) ingredient amount for display. Large amounts are rounded to avoid false
// precision (over 100 → nearest 10, over 50 → nearest 5); smaller amounts snap near-fractions to the
// common cooking vulgar-fraction glyphs (halves, thirds, quarters, sixths, eighths) — e.g. 0.5 → "½",
// 1.5 → "1½", 0.6667 → "⅔" — and otherwise fall back to a plain two-decimal number.
public static class AmountFormat
{
    // Half of the smallest gap between supported fractions is 1/48 ≈ 0.0208, so a 0.02 window snaps an
    // amount to a fraction only when it's unambiguously near exactly one of them.
    private const double Tolerance = 0.02;

    private static readonly (double Value, string Glyph)[] Fractions =
    [
        (1.0 / 8, "⅛"),
        (1.0 / 6, "⅙"),
        (1.0 / 4, "¼"),
        (1.0 / 3, "⅓"),
        (3.0 / 8, "⅜"),
        (1.0 / 2, "½"),
        (5.0 / 8, "⅝"),
        (2.0 / 3, "⅔"),
        (3.0 / 4, "¾"),
        (5.0 / 6, "⅚"),
        (7.0 / 8, "⅞"),
    ];

    public static string Format(decimal amount)
    {
        if (amount <= 0)
        {
            return "0";
        }

        // Larger amounts don't need fractional precision — round to a clean step.
        if (amount > 100)
        {
            return RoundToStep(amount, 10);
        }

        if (amount > 50)
        {
            return RoundToStep(amount, 5);
        }

        var whole = (long)decimal.Floor(amount);
        var frac = (double)(amount - whole);

        if (frac < Tolerance)
        {
            return whole.ToString(CultureInfo.InvariantCulture);
        }

        if (frac > 1 - Tolerance)
        {
            return (whole + 1).ToString(CultureInfo.InvariantCulture);
        }

        var nearest = Fractions.MinBy(f => Math.Abs(frac - f.Value));
        if (Math.Abs(frac - nearest.Value) <= Tolerance)
        {
            return whole > 0
                ? $"{whole.ToString(CultureInfo.InvariantCulture)}{nearest.Glyph}"
                : nearest.Glyph;
        }

        return decimal.Round(amount, 2).ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string RoundToStep(decimal amount, int step) =>
        (Math.Round(amount / step, MidpointRounding.AwayFromZero) * step).ToString(CultureInfo.InvariantCulture);
}
