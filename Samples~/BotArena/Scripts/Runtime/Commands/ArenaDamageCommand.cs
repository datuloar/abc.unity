using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    internal readonly struct ArenaDamageCommand : IActorCommand
    {
        public ArenaDamageCommand(float amount) => Amount = amount;

        public float Amount { get; }
    }
}
