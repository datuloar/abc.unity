using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Abc.Unity
{
    internal enum ActorModuleResolution
    {
        Missing,
        Found,
        Ambiguous
    }

    internal sealed class ActorModuleMap<TModule> where TModule : class, IActorModule
    {
        private const int DictionaryThreshold = 8;

        private readonly struct Slot
        {
            public Slot(int typeIndex, TModule module)
            {
                TypeIndex = typeIndex;
                Module = module;
            }

            public int TypeIndex { get; }
            public TModule Module { get; }
        }

        private readonly struct Resolution
        {
            public Resolution(ActorModuleResolution result, TModule module)
            {
                Result = result;
                Module = module;
            }

            public ActorModuleResolution Result { get; }
            public TModule Module { get; }
        }

        private Slot[] _slots = Array.Empty<Slot>();
        private Dictionary<Type, Resolution> _resolutions;
        private Dictionary<int, TModule> _modulesByType;
        private int _count;

        public int Count => _count;

        public bool TryAdd(TModule module, out TModule existing)
        {
            var moduleType = module.GetType();
            var typeIndex = ActorModuleTypeRegistry<TModule>.GetOrCreate(moduleType);
            if (TryGetByTypeIndex(typeIndex, out existing))
                return false;

            EnsureModuleCapacity(_count + 1);
            _slots[_count] = new Slot(typeIndex, module);
            _count++;
            AddToLargeLookup(typeIndex, module);
            _resolutions?.Clear();
            return true;
        }

        public bool Remove(TModule module)
        {
            var typeIndex = ActorModuleTypeRegistry<TModule>.GetOrCreate(module.GetType());
            var index = FindTypeIndex(typeIndex);
            if (index < 0 || !ReferenceEquals(_slots[index].Module, module))
                return false;

            _count--;
            if (index < _count)
                Array.Copy(_slots, index + 1, _slots, index, _count - index);

            _slots[_count] = default;
            RemoveFromLargeLookup(typeIndex);
            _resolutions?.Clear();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ActorModuleResolution Resolve<TRequested>(out TModule module) where TRequested : class, TModule
        {
            var typeIndex = ActorModuleType<TModule, TRequested>.Index;
            if (TryGetByTypeIndex(typeIndex, out module))
            {
                return ActorModuleResolution.Found;
            }

            return ResolveSlow(typeof(TRequested), out module);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetExact<TRequested>(out TRequested module) where TRequested : class, TModule
        {
            var typeIndex = ActorModuleType<TModule, TRequested>.Index;
            if (TryGetByTypeIndex(typeIndex, out var result))
            {
                module = (TRequested)result;
                return true;
            }

            module = null;
            return false;
        }

        private ActorModuleResolution ResolveSlow(Type requestedType, out TModule module)
        {
            if (_resolutions != null && _resolutions.TryGetValue(requestedType, out var cached))
            {
                module = cached.Module;
                return cached.Result;
            }

            module = null;

            for (var i = 0; i < _count; i++)
            {
                var candidate = _slots[i].Module;
                if (!requestedType.IsAssignableFrom(candidate.GetType()))
                    continue;

                if (module != null)
                {
                    module = null;
                    CacheResolution(requestedType, ActorModuleResolution.Ambiguous, null);
                    return ActorModuleResolution.Ambiguous;
                }

                module = candidate;
            }

            if (module == null)
            {
                CacheResolution(requestedType, ActorModuleResolution.Missing, null);
                return ActorModuleResolution.Missing;
            }

            CacheResolution(requestedType, ActorModuleResolution.Found, module);
            return ActorModuleResolution.Found;
        }

        public bool ContainsReference(IActorModule module)
        {
            for (var i = 0; i < _count; i++)
            {
                if (ReferenceEquals(_slots[i].Module, module))
                    return true;
            }

            return false;
        }

        public void Clear()
        {
            Array.Clear(_slots, 0, _count);
            _count = 0;
            _resolutions?.Clear();
            _modulesByType = null;
        }

        private void CacheResolution(Type type, ActorModuleResolution resolution, TModule module)
        {
            _resolutions ??= new Dictionary<Type, Resolution>();
            _resolutions.Add(type, new Resolution(resolution, module));
        }

        private void EnsureModuleCapacity(int minimum)
        {
            if (_slots.Length >= minimum)
                return;

            var capacity = _slots.Length == 0 ? 1 : _slots.Length * 2;
            if (capacity < minimum)
                capacity = minimum;

            Array.Resize(ref _slots, capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetByTypeIndex(int typeIndex, out TModule module)
        {
            if (_modulesByType != null)
                return _modulesByType.TryGetValue(typeIndex, out module);

            for (var i = 0; i < _count; i++)
            {
                var slot = _slots[i];
                if (slot.TypeIndex == typeIndex)
                {
                    module = slot.Module;
                    return true;
                }
            }

            module = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindTypeIndex(int typeIndex)
        {
            for (var i = 0; i < _count; i++)
            {
                if (_slots[i].TypeIndex == typeIndex)
                    return i;
            }

            return -1;
        }

        private void AddToLargeLookup(int typeIndex, TModule module)
        {
            if (_modulesByType != null)
            {
                _modulesByType.Add(typeIndex, module);
                return;
            }

            if (_count <= DictionaryThreshold)
                return;

            _modulesByType = new Dictionary<int, TModule>(_count);
            for (var i = 0; i < _count; i++)
                _modulesByType.Add(_slots[i].TypeIndex, _slots[i].Module);
        }

        private void RemoveFromLargeLookup(int typeIndex)
        {
            if (_modulesByType == null)
                return;

            _modulesByType.Remove(typeIndex);
            if (_count <= DictionaryThreshold)
                _modulesByType = null;
        }
    }
}
