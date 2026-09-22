
namespace Abc.Unity
{
    public interface IActor : IReadOnlyActor, IActorTick, IActorFixedTick, IActorLateTick
    {
        void Initialize();

        void AddBlueprint(ActorBlueprint blueprint);

        void SendCommand<TCommand>(TCommand command = default) where TCommand : IActorCommand;

        void AddBehaviour<TBehaviour>(TBehaviour behaviour) where TBehaviour : class, IActorBehaviour;
        void RemoveBehaviour<TBehaviour>() where TBehaviour : class, IActorBehaviour;

        void AddData<TData>(TData data) where TData : class, IActorData;
        void RemoveData<TData>() where TData : class, IActorData;

        void SetTag(ActorTag tag);
        void Kill();
        void Revive();

        void Destroy();
    }
}
