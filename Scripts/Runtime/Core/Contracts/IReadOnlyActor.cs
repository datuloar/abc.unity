using abc.unity.Common;
using System;

namespace abc.unity.Core
{
    public interface IReadOnlyActor : IUnityView
    {
        IReadOnlyActorReactProperty<bool> IsAlive { get; }
        IReadOnlyActorReactProperty<ActorTag> Tag { get; }
        IReadOnlyActorReactProperty<bool> IsInitialized { get; }

        event Action Destroyed;

        bool HasBehaviour<TBehaviour>() where TBehaviour : IActorBehaviour;

        TData GetData<TData>() where TData : class, IActorData;
        bool HasData<TData>() where TData : IActorData;
        bool TryGetData<TData>(out TData data) where TData : class, IActorData;
    }
}