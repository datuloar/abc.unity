using abc.unity.Common;
using System;

namespace abc.unity.Core
{
    public interface IActor :
    IDisposable,
    ICommandListener,
    IBehaviourReceiver,
    IComponentsHolder,
    ITickable
    { }
}