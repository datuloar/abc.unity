using System;
using System.Runtime.CompilerServices;

namespace Abc.Unity
{
    public sealed partial class ActorWorld : IActorTick, IActorFixedTick, IActorLateTick, IDisposable
    {
        private ActorModel[] _actors = Array.Empty<ActorModel>();
        private ActorModel[] _pending = Array.Empty<ActorModel>();
        private int _slotCount;
        private int _pendingCount;
        private int _iterationDepth;
        private int _liveCount;
        private bool _requiresCompaction;
        private bool _clearing;
        private bool _disposed;

        public ActorWorld(int capacity = 0) : this("Actor World", capacity)
        {
        }

        public ActorWorld(string name, int capacity = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("World name cannot be empty.", nameof(name));

            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            Name = name;

            if (capacity > 0)
                _actors = new ActorModel[capacity];

#if UNITY_EDITOR
            ActorWorldDiagnostics.Register(this);
#endif
        }

        public string Name { get; }
        public int Count => _liveCount;
        public int Capacity => _actors.Length;
        public bool IsDisposed => _disposed;

        public void EnsureCapacity(int capacity)
        {
            EnsureAvailable();

            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            if (_actors.Length < capacity)
                Array.Resize(ref _actors, capacity);
        }

        public ActorModel Add(ActorModel actor)
        {
            EnsureAvailable();

            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            if (actor.World != null)
                throw new InvalidOperationException($"Actor {actor.Name} already belongs to a world.");

            actor.World = this;
            actor.IsActiveInWorld = false;

            if (_iterationDepth == 0)
            {
                EnsureCapacity(ref _actors, _slotCount + 1);
                actor.WorldIndex = _slotCount;
                actor.IsPendingInWorld = false;
                _actors[_slotCount++] = actor;
            }
            else
            {
                EnsureCapacity(ref _pending, _pendingCount + 1);
                actor.WorldIndex = _pendingCount;
                actor.IsPendingInWorld = true;
                _pending[_pendingCount++] = actor;
            }

            _liveCount++;

            try
            {
                actor.Initialize();

                if (!ReferenceEquals(actor.World, this))
                    return actor;

                if (!actor.IsPendingInWorld)
                {
                    actor.IsActiveInWorld = true;
                    RefreshDataIndexes(actor);
                }

                return actor;
            }
            catch
            {
                Remove(actor);
                throw;
            }
        }

        public bool Remove(ActorModel actor)
        {
            if (actor == null || !ReferenceEquals(actor.World, this))
                return false;

            var index = actor.WorldIndex;
            if (actor.IsActiveInWorld)
                RemoveFromDataIndexes(actor, index);

            actor.IsActiveInWorld = false;
            actor.World = null;
            actor.WorldIndex = -1;
            _liveCount--;

            if (actor.IsPendingInWorld)
                RemovePendingAt(index);
            else if (_iterationDepth == 0)
                RemoveActiveAtSwapBack(index);
            else
            {
                _actors[index] = null;
                _requiresCompaction = true;
            }

            actor.IsPendingInWorld = false;
            return true;
        }

        public bool Despawn(ActorModel actor)
        {
            if (!Contains(actor))
                return false;

            actor.Destroy();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(ActorModel actor) => actor != null && ReferenceEquals(actor.World, this);

        public void Tick(float deltaTime)
        {
            EnsureAvailable();
            var count = BeginIteration();

            try
            {
                for (var i = 0; i < count; i++)
                    _actors[i]?.Tick(deltaTime);
            }
            finally
            {
                EndIteration();
            }
        }

        public void FixedTick(float fixedDeltaTime)
        {
            EnsureAvailable();
            var count = BeginIteration();

            try
            {
                for (var i = 0; i < count; i++)
                    _actors[i]?.FixedTick(fixedDeltaTime);
            }
            finally
            {
                EndIteration();
            }
        }

        public void LateTick(float deltaTime)
        {
            EnsureAvailable();
            var count = BeginIteration();

            try
            {
                for (var i = 0; i < count; i++)
                    _actors[i]?.LateTick(deltaTime);
            }
            finally
            {
                EndIteration();
            }
        }

        public void Clear()
        {
            EnsureAvailable();
            ClearCore();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                ClearCore();
            }
            finally
            {
#if UNITY_EDITOR
                ActorWorldDiagnostics.Unregister(this);
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int BeginIteration()
        {
            _iterationDepth++;
            return _slotCount;
        }

        private void EndIteration()
        {
            _iterationDepth--;

            if (_iterationDepth == 0)
                FlushDeferredChanges();
        }

        private void FlushDeferredChanges()
        {
            if (_requiresCompaction)
            {
                var writeIndex = 0;

                for (var readIndex = 0; readIndex < _slotCount; readIndex++)
                {
                    var actor = _actors[readIndex];
                    if (actor == null)
                        continue;

                    _actors[writeIndex] = actor;
                    MoveDataIndexSlot(actor.WorldIndex, writeIndex);
                    actor.WorldIndex = writeIndex++;
                }

                Array.Clear(_actors, writeIndex, _slotCount - writeIndex);
                _slotCount = writeIndex;
                _requiresCompaction = false;
            }

            if (_pendingCount == 0)
                return;

            EnsureCapacity(ref _actors, _slotCount + _pendingCount);

            for (var i = 0; i < _pendingCount; i++)
            {
                var actor = _pending[i];
                _actors[_slotCount] = actor;
                actor.WorldIndex = _slotCount++;
                actor.IsPendingInWorld = false;
                actor.IsActiveInWorld = true;
                RefreshDataIndexes(actor);
                _pending[i] = null;
            }

            _pendingCount = 0;
        }

        private void RemoveActiveAtSwapBack(int index)
        {
            var lastIndex = --_slotCount;
            var replacement = _actors[lastIndex];
            _actors[lastIndex] = null;

            if (index == lastIndex)
                return;

            _actors[index] = replacement;
            MoveDataIndexSlot(lastIndex, index);
            replacement.WorldIndex = index;
        }

        private void RemovePendingAt(int index)
        {
            var lastIndex = --_pendingCount;
            var replacement = _pending[lastIndex];
            _pending[lastIndex] = null;

            if (index == lastIndex)
                return;

            _pending[index] = replacement;
            replacement.WorldIndex = index;
        }

        private void EnsureAvailable()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ActorWorld));

            if (_clearing)
                throw new InvalidOperationException("Actors cannot be added or ticked while the world is clearing.");
        }

        private static void EnsureCapacity(ref ActorModel[] array, int minimum)
        {
            if (array.Length >= minimum)
                return;

            var capacity = array.Length == 0 ? 4 : array.Length * 2;
            if (capacity < minimum)
                capacity = minimum;

            Array.Resize(ref array, capacity);
        }
    }
}
