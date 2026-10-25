namespace FunctionalBlog.Test;

// Builds a ConfigurationCache synchronously for test Envs. The in-memory repository completes
// synchronously, so blocking on its ValueTasks is safe here.
public static class TestConfiguration
{
    public static ConfigurationCache With(params (string Key, string Value)[] values)
    {
        var repo = new InMemoryConfigurationRepository();
        foreach (var (key, value) in values)
        {
            repo.Set(key, value).AsTask().GetAwaiter().GetResult();
        }

        return ConfigurationCache.LoadAsync(repo).AsTask().GetAwaiter().GetResult();
    }
}
