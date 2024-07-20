using abc.unity.Common;
using System;

namespace abc.unity.Core
{
    public interface IReadOnlyActor : IUnityView
    {
        IActorReactProperty<bool> IsAlive { get; set; }
        IActorReactProperty<ActorTag> Tag { get; }

        event Action Destroyed;

        bool HasBehaviour<TBehaviour>() where TBehaviour : IActorBehaviour;

        TData GetData<TData>() where TData : class, IActorData;
        bool HasData<TData>() where TData : IActorData;
        bool TryGetData<TData>(out TData data) where TData : class, IActorData;
    }
}