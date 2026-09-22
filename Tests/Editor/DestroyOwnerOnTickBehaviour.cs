using Abc.Unity;

namespace Abc.Unity.Tests
{
    public sealed class DestroyOwnerOnTickBehaviour : IActorBehaviour, IActorTick
    {
        public IActor Owner { get; set; }
        public int TickCount { get; private set; }

        public void Tick(float deltaTime)
        {
            TickCount++;
            Owner.Destroy();
        }
    }
}
