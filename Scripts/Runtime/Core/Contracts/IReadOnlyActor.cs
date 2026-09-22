using System;

namespace Abc.Unity
{
    public interface IReadOnlyActor
    {
        string Name { get; }
        IReadOnlyActorReactProperty<bool> IsAlive { get; }
        IReadOnlyActorReactProperty<ActorTag> Tag { get; }
        IReadOnlyActorReactProperty<bool> IsInitialized { get; }

        event Action Destroyed;

        bool HasBehaviour<TBehaviour>() where TBehaviour : class, IActorBehaviour;
        TBehaviour GetBehaviour<TBehaviour>() where TBehaviour : class, IActorBehaviour;
        bool TryGetBehaviour<TBehaviour>(out TBehaviour behaviour) where TBehaviour : class, IActorBehaviour;

        TData GetData<TData>() where TData : class, IActorData;
        bool HasData<TData>() where TData : class, IActorData;
        bool TryGetData<TData>(out TData data) where TData : class, IActorData;
    }
}
