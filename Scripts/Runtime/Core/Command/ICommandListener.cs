public interface ICommandListener<TCommand> where TCommand : ICommand
{
    void ReactCommand(TCommand command);
}