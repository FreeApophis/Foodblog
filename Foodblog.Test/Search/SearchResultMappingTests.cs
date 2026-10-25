namespace Foodblog.Test.Search;

public sealed class SearchResultMappingTests
{
    [Fact]
    public void Maps_a_complete_hit_to_a_search_result()
    {
        var fields = new Dictionary<string, IReadOnlyList<string>>
        {
            ["type"] = ["article"],
            ["id"] = ["7"],
            ["title"] = ["Macarons"],
            ["body"] = ["Rezept für Macarons"],
        };

        if (LeanCorpusSearchIndex.ToSearchResult(fields, 1.5f, body => $"snippet:{body}") is not [var result])
        {
            Assert.Fail("Expected a search result for a complete hit.");
            return;
        }

        Assert.Equal("article", result.Type);
        Assert.Equal(7, result.Id);
        Assert.Equal("Macarons", result.Title);
        Assert.Equal("snippet:Rezept für Macarons", result.Snippet);
        Assert.Equal(1.5f, result.Score);
    }

    [Fact]
    public void Skips_a_hit_whose_stored_fields_are_missing_the_type()
    {
        // A churn race in LeanCorpus can hand back a hit whose stored fields are incomplete; such a
        // hit must be dropped, never surfaced — and reading it must never throw.
        var fields = new Dictionary<string, IReadOnlyList<string>>
        {
            ["id"] = ["7"],
            ["title"] = ["Macarons"],
        };

        Assert.True(LeanCorpusSearchIndex.ToSearchResult(fields, 1f, _ => string.Empty) is []);
    }

    [Fact]
    public void Skips_a_hit_whose_id_is_not_a_number()
    {
        var fields = new Dictionary<string, IReadOnlyList<string>>
        {
            ["type"] = ["article"],
            ["id"] = ["not-a-number"],
            ["title"] = ["Macarons"],
        };

        Assert.True(LeanCorpusSearchIndex.ToSearchResult(fields, 1f, _ => string.Empty) is []);
    }
}
