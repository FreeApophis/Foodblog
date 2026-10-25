namespace Foodblog.Domain.Roles;

public sealed record RecipeResource : IResource
{
    public string ResourceKey => "recipe";
}
