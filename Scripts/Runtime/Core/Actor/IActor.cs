using abc.unity.Common;

namespace abc.unity.Core
{
    public interface IActor :
    ICommandSender,
    IBehavioursHolder,
    IComponentsHolder,
    IDestroyableHandler<IActor>,
    ITickable,
    IFixedTickable
    { }
}