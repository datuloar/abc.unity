using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace Abc.Unity
{
    internal sealed class ActorCommandRegistry
    {
        private sealed class ListenerCollection
        {
            private readonly DeferredList<object> _listeners = new DeferredList<object>();

            public int Count => _listeners.Count;

            public void Add(object listener)
            {
                if (!_listeners.Contains(listener))
                    _listeners.Add(listener);
            }

            public void Remove(object listener) => _listeners.Remove(listener);

            public void Clear() => _listeners.Clear();

            public void Send<TCommand>(TCommand command, IActor owner) where TCommand : IActorCommand
            {
                var count = _listeners.BeginIteration();

                try
                {
                    for (var i = 0; i < count; i++)
                    {
                        var listener = _listeners[i];
                        if (listener is IActorCommandListener<TCommand> commandListener)
                        {
                            try
                            {
                                commandListener.ReactActorCommand(command);
                            }
                            catch (Exception exception)
                            {
                                Debug.LogException(exception, listener as UnityEngine.Object ?? owner as UnityEngine.Object);
                            }
                        }
                    }
                }
                finally
                {
                    _listeners.EndIteration();
                }
            }
        }

        private readonly struct ListenerEntry
        {
            public ListenerEntry(int typeIndex, ListenerCollection listeners)
            {
                TypeIndex = typeIndex;
                Listeners = listeners;
            }

            public int TypeIndex { get; }
            public ListenerCollection Listeners { get; }
        }

        private static class CommandType<TCommand> where TCommand : IActorCommand
        {
            public static readonly int Index = GetCommandTypeIndex(typeof(TCommand));
        }

        private static readonly Dictionary<Type, Type[]> CommandTypesByBehaviour = new Dictionary<Type, Type[]>();
        private static readonly Dictionary<Type, int> CommandTypeIndices = new Dictionary<Type, int>();
        private static readonly object CommandTypesLock = new object();

        private readonly IActor _owner;
        private ListenerEntry[] _listenerEntries = Array.Empty<ListenerEntry>();
        private int _listenerEntryCount;

        public ActorCommandRegistry(IActor owner) => _owner = owner;

        public static bool HandlesCommands(Type behaviourType) => GetCommandTypes(behaviourType).Length > 0;

        public void Register(IActorBehaviour behaviour)
        {
            var commandTypes = GetCommandTypes(behaviour.GetType());

            for (var i = 0; i < commandTypes.Length; i++)
            {
                var typeIndex = GetCommandTypeIndex(commandTypes[i]);
                var entryIndex = FindEntry(typeIndex);

                if (entryIndex < 0)
                {
                    EnsureEntryCapacity(_listenerEntryCount + 1);
                    entryIndex = _listenerEntryCount++;
                    _listenerEntries[entryIndex] = new ListenerEntry(typeIndex, new ListenerCollection());
                }

                _listenerEntries[entryIndex].Listeners.Add(behaviour);
            }
        }

        public void Unregister(IActorBehaviour behaviour)
        {
            var commandTypes = GetCommandTypes(behaviour.GetType());

            for (var i = 0; i < commandTypes.Length; i++)
            {
                var entryIndex = FindEntry(GetCommandTypeIndex(commandTypes[i]));
                if (entryIndex < 0)
                    continue;

                var listeners = _listenerEntries[entryIndex].Listeners;
                listeners.Remove(behaviour);

                if (listeners.Count == 0)
                    RemoveEntryAtSwapBack(entryIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send<TCommand>(TCommand command) where TCommand : IActorCommand
        {
            var entryIndex = FindEntry(CommandType<TCommand>.Index);
            if (entryIndex >= 0)
                _listenerEntries[entryIndex].Listeners.Send(command, _owner);
        }

        public void Clear()
        {
            for (var i = 0; i < _listenerEntryCount; i++)
                _listenerEntries[i].Listeners.Clear();

            Array.Clear(_listenerEntries, 0, _listenerEntryCount);
            _listenerEntryCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindEntry(int typeIndex)
        {
            for (var i = 0; i < _listenerEntryCount; i++)
            {
                if (_listenerEntries[i].TypeIndex == typeIndex)
                    return i;
            }

            return -1;
        }

        private void EnsureEntryCapacity(int minimum)
        {
            if (_listenerEntries.Length >= minimum)
                return;

            var capacity = _listenerEntries.Length == 0 ? 1 : _listenerEntries.Length * 2;
            if (capacity < minimum)
                capacity = minimum;

            Array.Resize(ref _listenerEntries, capacity);
        }

        private void RemoveEntryAtSwapBack(int index)
        {
            var lastIndex = --_listenerEntryCount;
            _listenerEntries[index] = _listenerEntries[lastIndex];
            _listenerEntries[lastIndex] = default;
        }

        private static int GetCommandTypeIndex(Type commandType)
        {
            lock (CommandTypesLock)
            {
                if (CommandTypeIndices.TryGetValue(commandType, out var index))
                    return index;

                index = CommandTypeIndices.Count;
                CommandTypeIndices.Add(commandType, index);
                return index;
            }
        }

        private static Type[] GetCommandTypes(Type behaviourType)
        {
            lock (CommandTypesLock)
            {
                if (CommandTypesByBehaviour.TryGetValue(behaviourType, out var cached))
                    return cached;

                var interfaces = behaviourType.GetInterfaces();
                List<Type> commandTypes = null;

                for (var i = 0; i < interfaces.Length; i++)
                {
                    var implementedInterface = interfaces[i];
                    if (!implementedInterface.IsGenericType ||
                        implementedInterface.GetGenericTypeDefinition() != typeof(IActorCommandListener<>))
                        continue;

                    commandTypes ??= new List<Type>();
                    var commandType = implementedInterface.GetGenericArguments()[0];

                    if (!commandTypes.Contains(commandType))
                        commandTypes.Add(commandType);
                }

                cached = commandTypes == null ? Type.EmptyTypes : commandTypes.ToArray();
                CommandTypesByBehaviour.Add(behaviourType, cached);
                return cached;
            }
        }
    }
}
