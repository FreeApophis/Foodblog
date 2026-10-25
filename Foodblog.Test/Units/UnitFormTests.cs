namespace Foodblog.Test.Units;

public sealed class UnitFormTests
{
    [Fact]
    public void Valid_form_returns_success_with_typed_fields()
    {
        var form = ValidatedAssert.IsSuccess(UnitForm.Decode(Build("Esslöffel", "EL", "1", "0.015")));

        Assert.Equal("Esslöffel", form.Name);
        Assert.Equal("EL", form.Abbreviation);
        Assert.Equal(UnitCategory.Volume, form.Category);
        Assert.Equal(0.015m, form.Factor);
    }

    [Fact]
    public void Empty_name_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(UnitForm.Decode(Build(string.Empty, "EL", "1", "1")));

        Assert.Contains("unit.error.name_required", errors);
    }

    [Fact]
    public void Empty_abbreviation_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(UnitForm.Decode(Build("Esslöffel", string.Empty, "1", "1")));

        Assert.Contains("unit.error.abbreviation_required", errors);
    }

    [Fact]
    public void Invalid_category_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(UnitForm.Decode(Build("Esslöffel", "EL", "99", "1")));

        Assert.Contains("unit.error.category_invalid", errors);
    }

    [Fact]
    public void Factor_of_zero_or_less_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(UnitForm.Decode(Build("Esslöffel", "EL", "1", "0")));

        Assert.Contains("unit.error.factor_invalid", errors);
    }

    [Fact]
    public void Factor_not_a_number_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(UnitForm.Decode(Build("Esslöffel", "EL", "1", "abc")));

        Assert.Contains("unit.error.factor_invalid", errors);
    }

    [Fact]
    public void Multiple_errors_accumulate()
    {
        var errors = ValidatedAssert.IsFailure(UnitForm.Decode(Build(string.Empty, string.Empty, "99", "0")));

        Assert.Contains("unit.error.name_required", errors);
        Assert.Contains("unit.error.abbreviation_required", errors);
        Assert.Contains("unit.error.category_invalid", errors);
        Assert.Contains("unit.error.factor_invalid", errors);
    }

    private static Request Build(string name, string abbreviation, string category, string factor)
    {
        var form = new Dictionary<string, string>
        {
            ["name"] = name,
            ["abbreviation"] = abbreviation,
            ["category"] = category,
            ["factor"] = factor,
        };
        return new Request(HttpMethod.Post, "/admin/units", Empty, Empty, form, Empty);
    }

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
