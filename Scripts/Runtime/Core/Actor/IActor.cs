using abc.unity.Common;

namespace abc.unity.Core
{
    public interface IActor :
    ICommandSender,
    IBehavioursHolder,
    IComponentsHolder,
    IBlueprintsHolder,
    IDeadable,
    IDestroyableHandler<IActor>,
    ITransformable,
    IInitializable,
    ITickable,
    IFixedTickable,
    ILateTickable
    {
        ActorTag Tag { get; }
    }
}