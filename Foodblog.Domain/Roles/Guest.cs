namespace Foodblog.Domain.Roles;

public sealed class Guest : IPrincipal
{
    public static readonly Guest Instance = new();

    private Guest()
    {
    }

    public bool IsAuthenticated => false;

    public bool Can<TAction>(IResource resource)
        where TAction : IAction => false;
}
