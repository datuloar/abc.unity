using abc.unity.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace abc.unity.Core
{
    [Il2CppSetOption(Option.NullChecks | Option.ArrayBoundsChecks, false)]
    [DefaultExecutionOrder(-99999)]
    public class Actor : MonoBehaviour, IActor
    {
        private readonly ActorFastList<ITickable> _tickables = new();
        private readonly ActorFastList<IFixedTickable> _fixedTickables = new();
        private readonly ActorFastList<ILateTickable> _lateTickables = new();

        private readonly Dictionary<Type, IActorData> _dataMap = new();
        private readonly Dictionary<Type, IActorBehaviour> _behavioursMap = new();
        private readonly Dictionary<Type, List<object>> _listenersMap = new(64);

        private readonly ActorReactProperty<bool> _isAlive = new();
        private readonly ActorReactProperty<bool> _isInitialized = new();

        [SerializeField] private ActorReactProperty<ActorTag> _tag;
        [SerializeField] private List<ActorBlueprint> _blueprints;
        [SerializeField] private bool _initializeOnAwake = true;
        [SerializeField] private bool _hasUpdate = true;
        [SerializeField] private bool _hasFixedUpdate = true;
        [SerializeField] private bool _hasLateUpdate = true;

        private bool _isDestroyed;

        public IReadOnlyActorReactProperty<bool> IsAlive => _isAlive;
        public IReadOnlyActorReactProperty<bool> IsInitialized => _isInitialized;
        public IReadOnlyActorReactProperty<ActorTag> Tag => _tag;

        public event Action Destroyed;

        private void Awake()
        {
            if (_initializeOnAwake)
                Initialize();
        }

        private void OnDestroy()
        {
            if (!_isDestroyed)
                CleanUp();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize()
        {
            if (_isInitialized.Value)
                return;

            foreach (var blueprint in _blueprints)
                AddBlueprint(blueprint);

            FetchModules();

            InitializeDataAndBehaviours(_dataMap.Values);
            InitializeDataAndBehaviours(_behavioursMap.Values);

            RegisterForUpdates();

            ActorsContainer.Add(this);

            _isAlive.Value = true;
            _isInitialized.Value = true;
        }

        private void RegisterForUpdates()
        {
            if (_hasUpdate)
                ActorsUpdateManager.AddTickable(this);

            if (_hasFixedUpdate)
                ActorsUpdateManager.AddFixedTickable(this);

            if (_hasLateUpdate)
                ActorsUpdateManager.AddLateTickable(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _tickables.Count; i++)
                _tickables[i].Tick(deltaTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FixedTick(float fixedDeltaTime)
        {
            for (int i = 0; i < _fixedTickables.Count; i++)
                _fixedTickables[i].FixedTick(fixedDeltaTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LateTick(float deltaTime)
        {
            for (int i = 0; i < _lateTickables.Count; i++)
                _lateTickables[i].LateTick(deltaTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddBlueprint(ActorBlueprint blueprint)
        {
            if (blueprint == null) return;

            foreach (var data in blueprint.Data)
                AddData(data.GetData());

            foreach (var behaviour in blueprint.Behaviour)
                AddBehaviour(behaviour.GetBehaviour());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasData<TData>() where TData : IActorData =>
            _dataMap.ContainsKey(typeof(TData));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddData<TData>(TData data) where TData : IActorData
        {
            var dataType = data.GetType();

            if (HasData<TData>())
            {
                Debug.LogError($"Data of type {dataType.Name} is already added to Actor-{gameObject.name}");
                return;
            }

            _dataMap.Add(dataType, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TData GetData<TData>() where TData : class, IActorData
        {
            if (_dataMap.TryGetValue(typeof(TData), out var data))
                return data as TData;

            throw new InvalidOperationException($"Actor-{gameObject.name} does not have data of type {typeof(TData).Name}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetData<TData>(out TData data) where TData : class, IActorData
        {
            if (_dataMap.TryGetValue(typeof(TData), out var result))
            {
                data = result as TData;
                return true;
            }

            data = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveData<TData>() where TData : IActorData => _dataMap.Remove(typeof(TData));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddBehaviour<TBehaviour>(TBehaviour behaviour) where TBehaviour : IActorBehaviour
        {
            var behaviourType = behaviour.GetType();
            _behavioursMap[typeof(TBehaviour)] = behaviour;
            behaviour.Owner = this;

            RegisterListeners(behaviour, behaviourType);

            if (behaviour is ITickable tickable)
                _tickables.Add(tickable);

            if (behaviour is IFixedTickable fixedTickable)
                _fixedTickables.Add(fixedTickable);

            if (behaviour is ILateTickable lateTickable)
                _lateTickables.Add(lateTickable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterListeners(IActorBehaviour behaviour, Type behaviourType)
        {
            var interfaces = behaviourType.GetInterfaces();

            foreach (var implementedInterface in interfaces)
            {
                if (implementedInterface.IsGenericType
                    && implementedInterface.GetGenericTypeDefinition() == typeof(IActorCommandListener<>))
                {
                    var commandType = implementedInterface.GetGenericArguments()[0];

                    if (!_listenersMap.TryGetValue(commandType, out var listeners))
                    {
                        listeners = new List<object>();
                        _listenersMap[commandType] = listeners;
                    }

                    listeners.Add(behaviour);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasBehaviour<TBehaviour>() where TBehaviour : IActorBehaviour =>
            _behavioursMap.ContainsKey(typeof(TBehaviour));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveBehaviour<TBehaviour>() where TBehaviour : IActorBehaviour
        {
            _behavioursMap.Remove(typeof(TBehaviour));

            foreach (var listeners in _listenersMap.Values)
                listeners.RemoveAll(listener => listener is TBehaviour);

            _tickables.RemoveAll(t => t is TBehaviour);
            _fixedTickables.RemoveAll(t => t is TBehaviour);
            _lateTickables.RemoveAll(t => t is TBehaviour);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendCommand<TCommand>(TCommand command = default) where TCommand : struct, IActorCommand
        {
            if (_listenersMap.TryGetValue(typeof(TCommand), out var listeners))
            {
                foreach (var listener in listeners)
                {
                    if (listener is IActorCommandListener<TCommand> commandListener)
                        commandListener.ReactActorCommand(command);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Kill() => _isAlive.Value = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Revive() => _isAlive.Value = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Destroy()
        {
            CleanUp();

            Destroyed?.Invoke();

            _isDestroyed = true;

            Destroy(gameObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FetchModules()
        {
            foreach (var module in GetComponentsInChildren<IActorModule>(true))
            {
                if (module is IActorData actorData)
                    AddData(actorData);

                if (module is IActorBehaviour actorBehaviour)
                    AddBehaviour(actorBehaviour);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeDataAndBehaviours<T>(IEnumerable<T> items) where T : IActorModule
        {
            foreach (var item in items)
            {
                item.PreInitialize();
                item.Initialize();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CleanUp()
        {
            if (_hasUpdate)
                ActorsUpdateManager.RemoveTickable(this);

            if (_hasFixedUpdate)
                ActorsUpdateManager.RemoveFixedTickable(this);

            if (_hasLateUpdate)
                ActorsUpdateManager.RemoveLateTickable(this);

            ActorsContainer.Remove(this);

            foreach (var behaviour in _behavioursMap.Values)
                behaviour.CleanUp();

            foreach (var data in _dataMap.Values)
                data.CleanUp();
        }
    }
}
