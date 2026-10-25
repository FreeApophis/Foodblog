namespace Foodblog.Test.Configuration;

public abstract class ConfigurationRepositoryContract
{
    [Fact]
    public async Task Get_returns_none_for_an_unset_key()
    {
        var repo = Create();

        FunctionalAssert.None(await repo.Get("does.not.exist"));
    }

    [Fact]
    public async Task Set_then_Get_round_trips_a_value()
    {
        var repo = Create();

        await repo.Set("site.name", "Mein Foodblog");

        Assert.Equal(Option.Some("Mein Foodblog"), await repo.Get("site.name"));
    }

    [Fact]
    public async Task Set_overwrites_an_existing_value()
    {
        var repo = Create();
        await repo.Set("site.name", "Alt");

        await repo.Set("site.name", "Neu");

        Assert.Equal(Option.Some("Neu"), await repo.Get("site.name"));
    }

    [Fact]
    public async Task All_returns_every_stored_pair()
    {
        var repo = Create();
        await repo.Set("site.name", "Blog");
        await repo.Set("smtp.host", "mail.example.com");

        var all = await repo.All();

        Assert.Equal("Blog", all["site.name"]);
        Assert.Equal("mail.example.com", all["smtp.host"]);
    }

    protected abstract IConfigurationRepository Create();
}
