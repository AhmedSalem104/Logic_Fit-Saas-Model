using LogicFit.Domain.ValueObjects;

namespace LogicFit.Application;

public sealed class GymContextAccessor : IGymContextAccessor
{
    public GymScope? Current { get; set; }
}
