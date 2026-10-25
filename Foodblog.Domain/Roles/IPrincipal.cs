namespace Foodblog.Domain.Roles;

public interface IPrincipal
{
    bool IsAuthenticated { get; }

    bool Can<TAction>(IResource resource)
        where TAction : IAction;
}
