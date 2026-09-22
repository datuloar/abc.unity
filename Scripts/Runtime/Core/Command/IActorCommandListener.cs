namespace Abc.Unity
{
    public interface IActorCommandListener<TCommand> where TCommand : IActorCommand
    {
        void ReactActorCommand(TCommand command);
    }
}
