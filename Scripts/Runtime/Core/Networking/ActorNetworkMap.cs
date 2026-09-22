using System;
using System.Collections.Generic;

namespace Abc.Unity
{
    public sealed class ActorNetworkMap : IDisposable
    {
        private sealed class Binding
        {
            private readonly ActorNetworkMap _map;

            public Binding(ActorNetworkMap map, ActorNetworkId id, IActor actor)
            {
                _map = map;
                Id = id;
                Actor = actor;
                OnDestroyed = Remove;
            }

            public ActorNetworkId Id { get; }
            public IActor Actor { get; }
            public Action OnDestroyed { get; }

            private void Remove() => _map.RemoveDestroyedBinding(this);
        }

        private readonly Dictionary<ActorNetworkId, Binding> _byId;
        private readonly Dictionary<IActor, Binding> _byActor;

        public ActorNetworkMap(int capacity = 0)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _byId = new Dictionary<ActorNetworkId, Binding>(capacity);
            _byActor = new Dictionary<IActor, Binding>(capacity, ReferenceEqualityComparer<IActor>.Instance);
        }

        public int Count => _byId.Count;
        public bool IsDisposed { get; private set; }

        public void Bind(ActorNetworkId id, IActor actor)
        {
            EnsureNotDisposed();
            if (!id.IsValid)
                throw new ArgumentException("A valid network ID is required.", nameof(id));
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (!actor.IsInitialized.Value)
                throw new InvalidOperationException("Initialize the actor before binding its network ID.");
            if (_byId.ContainsKey(id) || _byActor.ContainsKey(actor))
                throw new InvalidOperationException("An actor and its network ID must have exactly one binding per map.");

            var binding = new Binding(this, id, actor);
            _byId.Add(id, binding);
            try
            {
                _byActor.Add(actor, binding);
                actor.Destroyed += binding.OnDestroyed;
            }
            catch
            {
                _byId.Remove(id);
                _byActor.Remove(actor);
                actor.Destroyed -= binding.OnDestroyed;
                throw;
            }
        }

        public bool TryGetActor(ActorNetworkId id, out IActor actor)
        {
            if (_byId.TryGetValue(id, out var binding) && binding.Actor.IsInitialized.Value)
            {
                actor = binding.Actor;
                return true;
            }

            actor = null;
            return false;
        }

        public bool TryGetId(IActor actor, out ActorNetworkId id)
        {
            if (actor != null && _byActor.TryGetValue(actor, out var binding) && actor.IsInitialized.Value)
            {
                id = binding.Id;
                return true;
            }

            id = default;
            return false;
        }

        public bool Unbind(ActorNetworkId id)
        {
            if (!_byId.TryGetValue(id, out var binding))
                return false;

            _byId.Remove(id);
            _byActor.Remove(binding.Actor);
            binding.Actor.Destroyed -= binding.OnDestroyed;
            return true;
        }

        public void Clear()
        {
            foreach (var binding in _byId.Values)
                binding.Actor.Destroyed -= binding.OnDestroyed;

            _byId.Clear();
            _byActor.Clear();
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            Clear();
            IsDisposed = true;
        }

        private void RemoveDestroyedBinding(Binding binding)
        {
            if (_byId.TryGetValue(binding.Id, out var current) && ReferenceEquals(current, binding))
                Unbind(binding.Id);
        }

        private void EnsureNotDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(ActorNetworkMap));
        }
    }
}
