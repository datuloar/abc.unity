using System;

namespace Abc.Unity
{
    public sealed class ActorSimulation
    {
        private readonly Action<float> _simulatePhysics;
        private bool _stepping;

        public ActorSimulation(ActorWorld world, float fixedDeltaTime, Action<float> simulatePhysics = null)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            if (world.IsDisposed)
                throw new ObjectDisposedException(nameof(world));
            if (float.IsNaN(fixedDeltaTime) || float.IsInfinity(fixedDeltaTime) || fixedDeltaTime <= 0f)
                throw new ArgumentOutOfRangeException(nameof(fixedDeltaTime));

            DeltaTime = fixedDeltaTime;
            _simulatePhysics = simulatePhysics;
        }

        public ActorWorld World { get; }
        public float DeltaTime { get; }
        public ulong Tick { get; private set; }
        public bool IsFaulted { get; private set; }

        public void Step()
        {
            EnsureCanStep();
            _stepping = true;
            try
            {
                World.FixedTick(DeltaTime);
                if (World.IsDisposed)
                    throw new ObjectDisposedException(nameof(World));

                _simulatePhysics?.Invoke(DeltaTime);
                if (World.IsDisposed)
                    throw new ObjectDisposedException(nameof(World));

                Tick++;
            }
            catch
            {
                IsFaulted = true;
                throw;
            }
            finally
            {
                _stepping = false;
            }
        }

        private void EnsureCanStep()
        {
            if (_stepping)
                throw new InvalidOperationException("Simulation steps cannot be nested.");
            if (IsFaulted)
                throw new InvalidOperationException("A failed simulation must be replaced, not retried.");
            if (World.IsDisposed)
                throw new ObjectDisposedException(nameof(World));
            if (Tick == ulong.MaxValue)
                throw new InvalidOperationException("The simulation tick counter is exhausted.");
        }
    }
}
