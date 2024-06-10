using abc.unity.Common;
using System;

namespace abc.unity.Core
{
    public interface IReadOnlyActor : IUnityView
    {
        ActorTag Tag { get; }
        bool IsAlive { get; }

        event Action Initialized;
        event Action Destroyed;
        event Action AliveStateChanged;

        bool HasBehaviour<TBehaviour>() where TBehaviour : IActorBehaviour;

        TData GetData<TData>() where TData : class, IActorData;
        bool HasData<TData>() where TData : IActorData;
        bool TryGetData<TData>(out TData data) where TData : class, IActorData;
    }
}