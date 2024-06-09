using abc.unity.Common;
using System;

namespace abc.unity.Core
{
    public interface IActor : IReadOnlyActor, ITickable, IFixedTickable, ILateTickable
    {
        void Initialize();

        void AddBlueprint(ActorBlueprint blueprint);

        void SendCommand<TCommand>(TCommand command = default) where TCommand : struct, IActorCommand;

        void AddBehaviour<TBehaviour>(TBehaviour behaviour) where TBehaviour : IActorBehaviour;
        void RemoveBehaviour<TBehaviour>() where TBehaviour : IActorBehaviour;

        void AddData<TData>(TData data) where TData : IActorData;
        void RemoveData<TData>() where TData : IActorData;

        void ChangeAliveState(bool isDead);

        void Destroy();
    }

    public interface IReadOnlyActor : IUnityView
    {
        ActorTag Tag { get; }
        bool IsAlive { get; }


        public event Action Destroyed;

        bool HasBehaviour<TBehaviour>() where TBehaviour : IActorBehaviour;

        TData GetData<TData>() where TData : class, IActorData;
        bool HasData<TData>() where TData : IActorData;
        bool TryGetData<TData>(out TData data) where TData : class, IActorData;
    }
}