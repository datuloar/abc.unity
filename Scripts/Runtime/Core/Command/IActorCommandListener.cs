namespace abc.unity.Core
{
    public interface IActorCommandListener<TCommand> where TCommand : IActorCommand
    {
        void ReactActorCommand(TCommand command);
    }
}