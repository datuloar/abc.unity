using System;

namespace abc.unity.Core
{
    public interface IActor :
    IDisposable,
    ICommandListener,
    IBehaviourReceiver,
    IComponentsProvider,
    ITickable
    { }
}