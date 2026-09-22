using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    internal sealed class ArenaHealthBehaviour : IActorBehaviour, IActorCommandListener<ArenaDamageCommand>
    {
        private readonly ArenaSession _session;
        private ArenaHealthData _health;
        private ArenaVisualData _visual;

        public ArenaHealthBehaviour(ArenaSession session) => _session = session;

        public IActor Owner { get; set; }

        public void Initialize()
        {
            _health = Owner.GetData<ArenaHealthData>();
            _visual = Owner.GetData<ArenaVisualData>();
            _visual.SetHealth(_health.Normalized);
        }

        public void ReactActorCommand(ArenaDamageCommand command)
        {
            var died = _health.ApplyDamage(command.Amount);
            _visual.SetHealth(_health.Normalized);

            if (died)
                _session.OnAgentKilled((ActorModel)Owner);
        }
    }
}
