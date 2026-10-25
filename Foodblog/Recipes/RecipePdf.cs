using System.Globalization;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace Foodblog.Recipes;

// Renders a recipe to an A4 PDF in one of the two designs (C1 default / C3 alternative), reproducing
// the Foodblog "Recipe Book" layouts in QuestPDF. The design fonts (Newsreader / Hanken Grotesk /
// JetBrains Mono) are embedded as TTFs and registered on first use. Cover images are centre-cropped
// with SkiaSharp so they fill their box (QuestPDF only offers contain-style fitting).
public static class RecipePdf
{
    private const string Serif = "Newsreader";
    private const string Sans = "Hanken Grotesk";
    private const string Mono = "JetBrains Mono";

    private const string Paper = "#f4eede";
    private const string Surface = "#fffefb";
    private const string Ink = "#2b2622";
    private const string Muted = "#6f6557";
    private const string Muted2 = "#8c8175";
    private const string Excerpt = "#5f564a";
    private const string Tomato = "#c0533b";
    private const string Green = "#5b7b53";
    private const string GreenDark = "#3f5a39";
    private const string Dark = "#2b2622";
    private const string OnDark = "#b6a98e";
    private const string Cream = "#f4eede";
    private const string Sand = "#e8c9a8";

    private const float HeroRatio = 595.28f / 250f;
    private const float PhotoRatio = 215f / 265f;

    private const string BrandLine = "FOODBLOG";

    // Chef's-hat mark (a stand-in for the penguin banner logo we have no clean vector of).
    private const string ChefHatInner =
        """<g fill="#5b7b53"><circle cx="8.4" cy="10" r="2.7"/><circle cx="12" cy="8.6" r="3.1"/><circle cx="15.6" cy="10" r="2.7"/><rect x="7.3" y="10" width="9.4" height="5.4"/><rect x="8.1" y="15" width="7.8" height="2.7" rx="0.5"/></g>""";

    private const string ClockInner =
        """<g fill="none" stroke="#8c8175" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9"/><path d="M12 7.5V12l3 1.8"/></g>""";

    private const string PersonInner =
        """<g fill="none" stroke="#8c8175" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="8" r="3.4"/><path d="M5.5 20a6.5 6.5 0 0 1 13 0"/></g>""";

    static RecipePdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var assembly = typeof(RecipePdf).Assembly;
        foreach (var resource in assembly.GetManifestResourceNames())
        {
            if (resource.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
            {
                using var stream = assembly.GetManifestResourceStream(resource);
                if (stream is not null)
                {
                    FontManager.RegisterFontFromStream(stream);
                }
            }
        }
    }

    public static byte[] Generate(RecipePdfModel model, RecipePdfDesign design, Translate t) =>
        BuildDocument(model, design, t).GeneratePdf();

    private static Document BuildDocument(RecipePdfModel model, RecipePdfDesign design, Translate t) =>
        Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(x => x.FontFamily(Sans).FontColor(Ink).FontSize(10).LineHeight(1.25f));

            // Content fills the page (its cream background covers the gap above the footer); the footer
            // bar is pinned to the page bottom.
            page.Content().Background(Paper).Element(content =>
            {
                if (design == RecipePdfDesign.Alternative)
                {
                    ComposeAlternative(content, model, t);
                }
                else
                {
                    ComposeDefault(content, model, t);
                }
            });

            page.Footer().Element(footer => FooterBar(
                footer,
                design == RecipePdfDesign.Alternative ? GreenDark : Dark,
                design == RecipePdfDesign.Alternative ? "#d7cba6" : OnDark));
        }));

    // C1 — Original · Tomate: full-bleed photo hero with the title overlaid, ingredients card + steps.
    private static void ComposeDefault(IContainer container, RecipePdfModel model, Translate t)
    {
        container.Column(col =>
        {
            col.Item().Height(250).Element(hero => Hero(hero, model));

            col.Item().PaddingHorizontal(34).PaddingTop(20).Element(meta => MetaRow(meta, model));

            col.Item().PaddingHorizontal(34).PaddingTop(16).PaddingBottom(24).Row(row =>
            {
                row.ConstantItem(210).Element(c => IngredientsCard(c, model, t, Tomato, boxed: true));
                row.Spacing(26);
                row.RelativeItem().Element(c => Steps(c, model, t, Tomato));
            });
        });
    }

    // C3 — Seitliches Hero · Grün: split top (green text panel + tall photo), dark stat strip, body.
    private static void ComposeAlternative(IContainer container, RecipePdfModel model, Translate t)
    {
        container.Column(col =>
        {
            col.Item().Height(265).Row(row =>
            {
                row.RelativeItem().Background(GreenDark).Layers(layers =>
                {
                    layers.PrimaryLayer().PaddingHorizontal(30).PaddingBottom(28).AlignBottom().Column(bottom =>
                    {
                        bottom.Item().Text(model.Eyebrow).FontFamily(Mono).FontSize(8).FontColor(Sand).LetterSpacing(0.14f);
                        bottom.Item().PaddingTop(8).Text(model.Title).FontFamily(Serif).FontSize(26).SemiBold().FontColor(Cream).LineHeight(1.05f);
                        bottom.Item().PaddingTop(12).Row(line =>
                        {
                            line.AutoItem().AlignMiddle().Text($"von {model.AuthorName}").FontFamily(Serif).Italic().FontSize(12).FontColor(Cream);
                            line.ConstantItem(10);
                            line.AutoItem().AlignMiddle().Background(Tomato).CornerRadius(9).PaddingHorizontal(8).PaddingVertical(2.5f)
                                .Text(model.DifficultyLabel).FontFamily(Mono).FontSize(7).FontColor(Cream).LetterSpacing(0.06f);
                        });
                    });
                    layers.Layer().Padding(30).Element(brand => Brand(brand, "#d7cba6"));
                });
                row.ConstantItem(215).Element(photo => Photo(photo, model.CoverImage, PhotoRatio, GreenDark, model.CoverFocal));
            });

            col.Item().Background(Dark).Row(row =>
            {
                StatCell(row, t("recipe.preparation"), $"{model.PreparationTime} {t("recipe.unit.minutes")}");
                StatCell(row, t("recipe.cooking"), $"{model.CookingTime} {t("recipe.unit.minutes")}");
                StatCell(row, t("recipe.calories"), $"{model.Calories} {t("recipe.unit.kcal")}");
                StatCell(row, t("recipe.portions"), model.Portions.ToString());
            });

            col.Item().PaddingHorizontal(30).PaddingVertical(24).Row(row =>
            {
                row.ConstantItem(200).Element(c => IngredientsCard(c, model, t, Green, boxed: false));
                row.Spacing(28);
                row.RelativeItem().Element(c => Steps(c, model, t, Green));
            });
        });
    }

    private static void Hero(IContainer container, RecipePdfModel model)
    {
        container.Layers(layers =>
        {
            if (CoverCrop(model.CoverImage, HeroRatio, model.CoverFocal) is { } bytes)
            {
                layers.PrimaryLayer().Image(bytes).FitArea();
            }
            else
            {
                layers.PrimaryLayer().Background(Dark);
            }

            // A vertical scrim over the whole photo — barely there at the top, deepening toward the
            // bottom (mirrors the design's gradient) — carrying the title so it stays legible. The
            // gradient is the background of this layer (behind the text), not a separate empty layer.
            layers.Layer()
                .BackgroundLinearGradient(
                90f,
                [
                    Color.FromHex("#0D140F0A"),
                    Color.FromHex("#26140F0A"),
                    Color.FromHex("#B3140F0A"),
                ])
                .PaddingHorizontal(28).PaddingBottom(28).AlignBottom().Column(c =>
                {
                    c.Item().Text(model.Eyebrow).FontFamily(Mono).FontSize(8).FontColor(Sand).LetterSpacing(0.16f);
                    c.Item().PaddingTop(8).Text(model.Title).FontFamily(Serif).FontSize(28).SemiBold().FontColor(Cream).LineHeight(1.05f);
                });

            layers.Layer().Padding(24).Element(brand => Brand(brand, Cream));
        });
    }

    // Brand lockup: the chef's-hat mark (vector SVG) + the "FOODBLOG" wordmark.
    private static void Brand(IContainer container, string textColor) =>
        container.Row(row =>
        {
            row.ConstantItem(13).Height(13).Svg(size => $"""
                <svg viewBox="0 0 24 24" width="{F(size.Width)}" height="{F(size.Height)}" xmlns="http://www.w3.org/2000/svg">
                    <circle cx="12" cy="12" r="12" fill="#fffefb" fill-opacity="0.94"/>
                    {ChefHatInner}
                </svg>
                """);
            row.ConstantItem(7);
            row.AutoItem().PaddingTop(3).Text(BrandLine).FontFamily(Mono).FontSize(7.5f).FontColor(textColor).LetterSpacing(0.18f);
        });

    // Renders a 24×24-viewBox icon scaled to its container.
    private static void Icon(IContainer container, string inner) =>
        container.Svg(size => $"""<svg viewBox="0 0 24 24" width="{F(size.Width)}" height="{F(size.Height)}" xmlns="http://www.w3.org/2000/svg">{inner}</svg>""");

    // A crisp vector dotted rule (a row of dots — no connecting line) that fills the row width.
    private static void DottedRule(IContainer container) =>
        container.Height(1.2f).Svg(size =>
        {
            const float spacing = 2.3f;
            const float radius = 0.6f;
            var cy = F(size.Height / 2f);
            var count = Math.Max(1, (int)(size.Width / spacing));
            var dots = string.Concat(Enumerable.Range(0, count).Select(i =>
                $"""<circle cx="{F((i * spacing) + radius)}" cy="{cy}" r="{F(radius)}" fill="#2b2622" fill-opacity="0.3"/>"""));
            return $"""<svg width="{F(size.Width)}" height="{F(size.Height)}" xmlns="http://www.w3.org/2000/svg">{dots}</svg>""";
        });

    private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static void Photo(IContainer container, byte[]? source, float ratio, string fallback, FocalPoint focal)
    {
        if (CoverCrop(source, ratio, focal) is { } data)
        {
            container.Image(data).FitArea();
        }
        else
        {
            container.Background(fallback);
        }
    }

    private static void MetaRow(IContainer container, RecipePdfModel model)
    {
        container.Row(row =>
        {
            row.AutoItem().AlignMiddle().Text($"von {model.AuthorName}").FontSize(10.5f).FontColor(Muted);
            row.ConstantItem(10);
            row.AutoItem().AlignMiddle().Background(Green).CornerRadius(9).PaddingHorizontal(8).PaddingVertical(2.5f)
                .Text(model.DifficultyLabel).FontFamily(Mono).FontSize(7).FontColor(Cream).LetterSpacing(0.06f);

            row.RelativeItem().AlignRight().Element(stats => MetaStats(stats, model));
        });
    }

    // Right-hand info line: clock + prep/cook times, calories, person + portions.
    private static void MetaStats(IContainer container, RecipePdfModel model) =>
        container.AlignRight().Row(stats =>
        {
            stats.ConstantItem(10).AlignMiddle().Height(10).Element(i => Icon(i, ClockInner));
            stats.ConstantItem(5);
            stats.AutoItem().AlignMiddle().Text($"{model.PreparationTime} min + {model.CookingTime} min").FontFamily(Mono).FontSize(8.5f).FontColor(Muted2);
            stats.ConstantItem(13);
            stats.AutoItem().AlignMiddle().Text($"{model.Calories} kcal").FontFamily(Mono).FontSize(8.5f).FontColor(Muted2);
            stats.ConstantItem(13);
            stats.ConstantItem(10).AlignMiddle().Height(10).Element(i => Icon(i, PersonInner));
            stats.ConstantItem(5);
            stats.AutoItem().AlignMiddle().Text($"{model.Portions}").FontFamily(Mono).FontSize(8.5f).FontColor(Muted2);
        });

    private static void StatCell(RowDescriptor row, string label, string value) =>
        row.RelativeItem().BorderRight(1).BorderColor("#3a352d").PaddingVertical(11).PaddingHorizontal(18).Column(c =>
        {
            c.Item().Text(label.ToUpperInvariant()).FontFamily(Mono).FontSize(7).FontColor(OnDark).LetterSpacing(0.12f);
            c.Item().PaddingTop(2).Text(value).FontFamily(Serif).FontSize(15).SemiBold().FontColor(Cream);
        });

    private static void IngredientsCard(IContainer container, RecipePdfModel model, Translate t, string accent, bool boxed)
    {
        var card = boxed
            ? container.Background(Surface).Border(1).BorderColor("#e4dcca").CornerRadius(5).Padding(18)
            : container;

        card.Column(col =>
        {
            col.Item().Text(t("recipe.ingredients")).FontFamily(Serif).FontSize(16).SemiBold().FontColor(accent);
            col.Item().PaddingTop(2).Text($"{t("recipe.pdf.for")} {model.Portions} {t("recipe.portions")}".ToUpperInvariant())
                .FontFamily(Mono).FontSize(7).FontColor(Muted2).LetterSpacing(0.1f);

            col.Item().PaddingTop(10).Column(list =>
            {
                for (var i = 0; i < model.Ingredients.Count; i++)
                {
                    var ingredient = model.Ingredients[i];
                    list.Item().PaddingVertical(3).Row(row =>
                    {
                        row.ConstantItem(48).Text(ingredient.Amount).FontFamily(Mono).FontSize(9).FontColor(accent);
                        row.RelativeItem().Text(ingredient.Name).FontSize(9.5f).FontColor(Ink);
                    });

                    if (i < model.Ingredients.Count - 1)
                    {
                        list.Item().Element(DottedRule);
                    }
                }
            });
        });
    }

    private static void Steps(IContainer container, RecipePdfModel model, Translate t, string accent)
    {
        container.Column(col =>
        {
            col.Item().Text(t("recipe.instructions")).FontFamily(Serif).FontSize(16).SemiBold().FontColor(Ink);

            col.Item().PaddingTop(12).Column(steps =>
            {
                var number = 0;
                foreach (var step in model.Steps)
                {
                    number++;
                    steps.Item().PaddingBottom(11).Row(row =>
                    {
                        row.ConstantItem(28).Text(number.ToString()).FontFamily(Serif).FontSize(22).SemiBold().FontColor(accent);
                        row.RelativeItem().AlignMiddle().Text(step).FontSize(9.5f).FontColor(Excerpt).LineHeight(1.45f);
                    });
                }
            });

            if (model.Tips.Count > 0)
            {
                // The tip's accent rule is always green in both designs.
                col.Item().PaddingTop(6).Background(Surface).BorderLeft(3).BorderColor(Green).CornerRadius(4).Padding(12).Text(text =>
                {
                    text.Span($"{t("recipe.pdf.tip")} · ").SemiBold().FontColor(Ink).FontSize(9.5f);
                    text.Span(string.Join(" ", model.Tips)).FontColor(Excerpt).FontSize(9.5f).LineHeight(1.45f);
                });
            }
        });
    }

    private static void FooterBar(IContainer container, string background, string color) =>
        container.Background(background).PaddingVertical(11).PaddingHorizontal(34)
            .Text("FOODBLOG.CH — SELBER MACHEN").FontFamily(Mono).FontSize(7.5f).FontColor(color).LetterSpacing(0.12f);

    // Crops the image to exactly the target width:height ratio so it fills its box without distortion
    // (cover), positioning the crop window around the focal point (matching the web slider) rather
    // than always the centre; returns null when there is no image.
    private static byte[]? CoverCrop(byte[]? source, float targetRatio, FocalPoint focal)
    {
        if (source is null)
        {
            return null;
        }

        using var bitmap = SKBitmap.Decode(source);
        if (bitmap is null)
        {
            return source;
        }

        var sourceRatio = (float)bitmap.Width / bitmap.Height;
        int cropWidth, cropHeight;
        if (sourceRatio > targetRatio)
        {
            cropHeight = bitmap.Height;
            cropWidth = (int)MathF.Round(cropHeight * targetRatio);
        }
        else
        {
            cropWidth = bitmap.Width;
            cropHeight = (int)MathF.Round(cropWidth / targetRatio);
        }

        if (cropWidth < 1 || cropHeight < 1)
        {
            return source;
        }

        var left = focal.CropLeft(bitmap.Width, cropWidth);
        var top = focal.CropTop(bitmap.Height, cropHeight);

        using var subset = new SKBitmap(cropWidth, cropHeight);
        if (!bitmap.ExtractSubset(subset, new SKRectI(left, top, left + cropWidth, top + cropHeight)))
        {
            return source;
        }

        using var image = SKImage.FromBitmap(subset);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 92);
        return data.ToArray();
    }
}
