namespace Foodblog.Test.Translations;

public sealed class InMemoryTranslationRepositoryTests : TranslationRepositoryContract
{
    protected override ITranslationRepository CreateRepository() => new InMemoryTranslationRepository();
}
