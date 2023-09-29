using abc.unity.Common;
using System;

namespace abc.unity.Core
{
    public interface IActor :
    IDisposable,
    ICommandListener,
    IBehavioursHolder,
    IComponentsHolder,
    ITickable
    { }
}