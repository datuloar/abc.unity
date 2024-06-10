using abc.unity.Common;

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
}