namespace Foodblog.Domain.Roles;

public sealed record IngredientResource : IResource
{
    public string ResourceKey => "ingredient";
}
