using abc.unity.Common;

namespace abc.unity.Core
{
    public interface IActor :
    ICommandListener,
    IBehavioursHolder,
    IComponentsHolder,
    ITickable
    { }
}