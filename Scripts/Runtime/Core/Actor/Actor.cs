using abc.unity.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace abc.unity.Core
{
    [DefaultExecutionOrder(-99999)]
    public class Actor : MonoBehaviour, IActor
    {
        private readonly List<ITickable> _tickables = new(100);
        private readonly List<IFixedTickable> _fixedTickables = new(100);
        private readonly List<ILateTickable> _lateTickables = new(100);
        private readonly List<IActorData> _data = new(100);
        private readonly List<IActorBehaviour> _behaviours = new(100);
        private readonly Dictionary<Type, List<object>> _listenersMap = new(100);

        [SerializeField] private ActorReactProperty<ActorTag> _tag;
        [SerializeField] private List<ActorBlueprint> _blueprints;
        [SerializeField] private bool _initializeOnAwake = true;
        [SerializeField] private bool _hasUpdate = true;
        [SerializeField] private bool _hasFixedUpdate = true;
        [SerializeField] private bool _hasLateUpdate = true;

        private bool _isDestroyed;
        private readonly ActorReactProperty<bool> _isAlive = new();
        private readonly ActorReactProperty<bool> _isInitialized = new();

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
            if (_isDestroyed)
                return;

            CleanUp();
        }

        public void Initialize()
        {
            if (_isInitialized.Value)
                return;

            for (int i = 0; i < _blueprints.Count; i++)
                AddBlueprint(_blueprints[i]);

            FetchModules();

            InitializeDataAndBehaviours(_data);
            InitializeDataAndBehaviours(_behaviours);

            if (_hasUpdate)
                ActorsUpdateManager.AddTickable(this);

            if (_hasFixedUpdate)
                ActorsUpdateManager.AddFixedTickable(this);

            if (_hasLateUpdate)
                ActorsUpdateManager.AddLateTickable(this);

            ActorsContainer.Add(this);

            _isAlive.Value = true;
            _isInitialized.Value = true;
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _tickables.Count; i++)
                _tickables[i].Tick(deltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            for (int i = 0; i < _fixedTickables.Count; i++)
                _fixedTickables[i].FixedTick(fixedDeltaTime);
        }

        public void LateTick(float deltaTime)
        {
            for (int i = 0; i < _lateTickables.Count; i++)
                _lateTickables[i].LateTick(deltaTime);
        }

        public void AddBlueprint(ActorBlueprint blueprint)
        {
            if (blueprint == null)
                return;

            foreach (var data in blueprint.Data)
                AddData(data.GetData());

            foreach (var behaviour in blueprint.Behaviour)
                AddBehaviour(behaviour.GetBehaviour());
        }

        public bool HasData<TData>() where TData : IActorData
        {
            for (int i = 0; i < _data.Count; i++)
            {
                if (_data[i] is TData)
                    return true;
            }

            return false;
        }

        public void AddData<TData>(TData data) where TData : IActorData
        {
            if (_data.Contains(data))
                return;

            _data.Add(data);
        }

        public void RemoveData<TData>() where TData : IActorData
        {
            for (int i = _data.Count - 1; i >= 0; i--)
            {
                if (_data[i] is TData)
                    _data.RemoveAt(i);
            }
        }

        public TData GetData<TData>() where TData : class, IActorData
        {
            for (int i = 0; i < _data.Count; i++)
            {
                if (_data[i] is TData typedData)
                    return typedData;
            }

            throw new InvalidOperationException($"Actor-{gameObject.name} does not have a data of type {typeof(TData).Name}");
        }

        public bool TryGetData<TData>(out TData data) where TData : class, IActorData
        {
            for (int i = 0; i < _data.Count; i++)
            {
                if (_data[i] is TData typedData)
                {
                    data = typedData;
                    return true;
                }
            }

            data = null;
            return false;
        }

        public void AddBehaviour<TBehaviour>(TBehaviour behaviour) where TBehaviour : IActorBehaviour
        {
            if (_behaviours.Contains(behaviour))
                return;

            _behaviours.Add(behaviour);
            behaviour.Owner = this;

            var behaviourType = behaviour.GetType();
            var interfaces = behaviourType.GetInterfaces();

            for (int i = 0; i < interfaces.Length; i++)
            {
                var implementedInterface = interfaces[i];
                if (implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == typeof(IActorCommandListener<>))
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

            if (behaviour is ITickable tickable)
                _tickables.Add(tickable);

            if (behaviour is IFixedTickable fixedTickable)
                _fixedTickables.Add(fixedTickable);

            if (behaviour is ILateTickable lateTickable)
                _lateTickables.Add(lateTickable);
        }

        public bool HasBehaviour<TBehaviour>() where TBehaviour : IActorBehaviour
        {
            for (int i = 0; i < _behaviours.Count; i++)
            {
                if (_behaviours[i] is TBehaviour)
                    return true;
            }

            return false;
        }

        public void RemoveBehaviour<TBehaviour>() where TBehaviour : IActorBehaviour
        {
            for (int i = _behaviours.Count - 1; i >= 0; i--)
            {
                if (_behaviours[i] is TBehaviour)
                    _behaviours.RemoveAt(i);
            }

            foreach (var listeners in _listenersMap.Values)
            {
                for (int i = listeners.Count - 1; i >= 0; i--)
                {
                    if (listeners[i] is TBehaviour)
                        listeners.RemoveAt(i);
                }
            }

            for (int i = _tickables.Count - 1; i >= 0; i--)
            {
                if (_tickables[i] is TBehaviour)
                    _tickables.RemoveAt(i);
            }

            for (int i = _fixedTickables.Count - 1; i >= 0; i--)
            {
                if (_fixedTickables[i] is TBehaviour)
                    _fixedTickables.RemoveAt(i);
            }

            for (int i = _lateTickables.Count - 1; i >= 0; i--)
            {
                if (_lateTickables[i] is TBehaviour)
                    _lateTickables.RemoveAt(i);
            }
        }

        public void SendCommand<TCommand>(TCommand command = default) where TCommand : struct, IActorCommand
        {
            if (_listenersMap.TryGetValue(typeof(TCommand), out var listeners))
            {
                for (int i = 0; i < listeners.Count; i++)
                {
                    if (listeners[i] is IActorCommandListener<TCommand> commandListener)
                        commandListener.ReactActorCommand(command);
                }
            }
        }

        public void Kill() => _isAlive.Value = false;

        public void Revive() => _isAlive.Value = true;

        public void Destroy()
        {
            CleanUp();

            Destroyed?.Invoke();

            _isDestroyed = true;

            Destroy(gameObject);
        }

        private void FetchModules()
        {
            var modules = GetComponentsInChildren<IActorModule>(true);

            for (int i = 0; i < modules.Length; i++)
            {
                var module = modules[i];
                if (module is IActorData actorData)
                    AddData(actorData);

                if (module is IActorBehaviour actorBehaviour)
                    AddBehaviour(actorBehaviour);
            }
        }

        private void InitializeDataAndBehaviours<T>(List<T> items) where T : IActorModule
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.PreInitialize();
                item.Initialize();
            }
        }

        private void CleanUp()
        {
            if (_hasUpdate)
                ActorsUpdateManager.RemoveTickable(this);

            if (_hasFixedUpdate)
                ActorsUpdateManager.RemoveFixedTickable(this);

            if (_hasLateUpdate)
                ActorsUpdateManager.RemoveLateTickable(this);

            ActorsContainer.Remove(this);

            for (int i = 0; i < _behaviours.Count; i++)
                _behaviours[i].CleanUp();

            for (int i = 0; i < _data.Count; i++)
                _data[i].CleanUp();
        }
    }
}