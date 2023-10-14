namespace abc.unity.Core
{
    public interface ICommandSender
    {
        void SendCommand<TCommand>(TCommand command = default) where TCommand : struct, ICommand;
    }
}