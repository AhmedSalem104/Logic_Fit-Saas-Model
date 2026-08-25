namespace LogicFit.Application;

public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    public AuthenticatedUser? Current { get; set; }
}
