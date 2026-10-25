namespace Foodblog.Test.Configuration;

public sealed class InMemoryConfigurationRepositoryTests : ConfigurationRepositoryContract
{
    protected override IConfigurationRepository Create() => new InMemoryConfigurationRepository();
}
