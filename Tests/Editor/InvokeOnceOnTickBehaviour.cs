using System;

using Abc.Unity;

namespace Abc.Unity.Tests
{
    public sealed class InvokeOnceOnTickBehaviour : IActorBehaviour, IActorTick
    {
        private Action _action;

        public InvokeOnceOnTickBehaviour(Action action) => _action = action;

        public IActor Owner { get; set; }
        public int TickCount { get; private set; }

        public void Tick(float deltaTime)
        {
            TickCount++;
            var action = _action;
            _action = null;
            action?.Invoke();
        }
    }
}
