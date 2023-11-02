using abc.unity.Common;

namespace abc.unity.Core
{
    public interface IActor :
    ICommandSender,
    IBehavioursHolder,
    IComponentsHolder,
    IDeadable,
    IDestroyableHandler<IActor>,
    ITransformable,
    ITickable,
    IFixedTickable
    { }
}