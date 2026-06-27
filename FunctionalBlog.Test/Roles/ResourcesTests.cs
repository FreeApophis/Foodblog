namespace FunctionalBlog.Test.Roles;

public sealed class ResourcesTests
{
    // Guards against the role editor's resource dropdown drifting from the resources the app actually
    // guards (the bug where a hard-coded list omitted recipe, ingredient, unit, image, …). Adding a
    // new IResource without registering it in Resources.All fails here.
    [Fact]
    public void AllKeys_lists_every_concrete_IResource_in_the_domain()
    {
        var reflected = typeof(IResource).Assembly.GetTypes()
            .Where(type => typeof(IResource).IsAssignableFrom(type) && type is { IsInterface: false, IsAbstract: false })
            .Select(type => ((IResource)Activator.CreateInstance(type)!).ResourceKey)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(reflected, Resources.AllKeys);
    }
}
