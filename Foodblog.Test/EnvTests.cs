namespace Foodblog.Test;

public sealed class EnvTests
{
    [Fact]
    public void Ctx_carries_the_current_year_from_the_clock()
    {
        var env = BuildEnv() with { Clock = new FixedClock(new DateTimeOffset(2031, 3, 1, 12, 0, 0, TimeSpan.Zero)) };

        Assert.Equal(Option.Some(2031), env.Ctx.Year);
    }

    [Fact]
    public void Ctx_carries_the_configured_since_year_and_instagram_handle()
    {
        var env = BuildEnv() with
        {
            Config = TestConfiguration.With(
                (ConfigurationKeys.SiteSince, "2016"),
                (ConfigurationKeys.InstagramHandle, "the_cooking.couple_")),
        };

        Assert.Equal(Option.Some(2016), env.Ctx.SinceYear);
        Assert.Equal(Option.Some("the_cooking.couple_"), env.Ctx.InstagramHandle);
    }

    [Fact]
    public void Ctx_has_no_since_year_or_instagram_handle_without_a_configuration()
    {
        var ctx = BuildEnv().Ctx;

        FunctionalAssert.None(ctx.SinceYear);
        FunctionalAssert.None(ctx.InstagramHandle);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now => now;
    }

    private static Env BuildEnv() => new(
        Articles: new InMemoryArticleRepository(),
        Users: new InMemoryUserRepository(),
        Roles: new InMemoryRoleRepository(),
        Sessions: new InMemorySessionStore(),
        PasswordResets: new InMemoryPasswordResetTokenStore(),
        PasswordHasher: new Pbkdf2PasswordHasher(),
        Clock: new SystemClock(),
        Log: new ConsoleLog(),
        CurrentUser: Guest.Instance,
        Recipes: new InMemoryRecipeRepository(),
        Ingredients: new InMemoryIngredientRepository(),
        Units: new InMemoryUnitRepository(),
        Images: new InMemoryImageRepository(),
        Pages: new InMemoryPageRepository());
}
