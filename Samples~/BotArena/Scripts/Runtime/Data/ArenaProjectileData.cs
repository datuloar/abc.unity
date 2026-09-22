using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    internal sealed class ArenaProjectileData : IActorData
    {
        public ArenaProjectileData(bool fromPlayer, float damage, float lifetime)
        {
            FromPlayer = fromPlayer;
            Damage = damage;
            Lifetime = lifetime;
        }

        public bool FromPlayer { get; }
        public float Damage { get; }
        public float Lifetime { get; set; }
    }
}
