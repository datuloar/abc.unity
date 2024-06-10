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

        [SerializeField] private ActorTag _tag;
        [SerializeField] private List<ActorBlueprint> _blueprints;
        [SerializeField] private bool _initializeOnAwake = true;
        [SerializeField] private bool _hasUpdate = true;
        [SerializeField] private bool _hasFixedUpdate = true;
        [SerializeField] private bool _hasLateUpdate = true;

        private bool _isDestroyed;

        public ActorTag Tag => _tag;
        public bool IsAlive { get; private set; }
        public bool IsInitialized { get; private set; }

        public event Action Initialized  = delegate { };
        public event Action Destroyed = delegate { };
        public event Action AliveStateChanged = delegate { };

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
            if (IsInitialized)
                return;

            foreach (var blueprint in _blueprints)
                AddBlueprint(blueprint);

            FetchModules();

            foreach (var data in _data)
                data.PreInitialize();

            foreach (var data in _data)
                data.Initialize();

            foreach (var behaviour in _behaviours)
                behaviour.PreInitialize();

            foreach (var behaviour in _behaviours)
                behaviour.Initialize();

            if (_hasUpdate)
                ActorsUpdateManager.AddTickable(this);

            if (_hasFixedUpdate)
                ActorsUpdateManager.AddFixedTickable(this);

            if (_hasLateUpdate)
                ActorsUpdateManager.AddLateTickable(this);

            ActorsContainer.Add(this);

            foreach (var blueprint in _blueprints)
                AddBlueprint(blueprint);

            IsAlive = true;
            IsInitialized = true;

            Initialized.Invoke();
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

        public bool HasData<TData>() where TData : IActorData =>
            _data.Exists(d => d is TData);

        public void AddData<TData>(TData data) where TData : IActorData
        {
            if (!HasData<TData>())
                _data.Add(data);
        }

        public void RemoveData<TData>() where TData : IActorData =>
            _data.RemoveAll(c => c is TData);

        public TData GetData<TData>() where TData : class, IActorData
        {
            var data = _data.Find(d => d is TData) as TData;

            if (data == null)
                throw new InvalidOperationException($"Actor-{gameObject.name} does not have a data of type {typeof(TData).Name}");

            return data;
        }

        public bool TryGetData<TData>(out TData data) where TData : class, IActorData
        {
            data = _data.Find(d => d is TData) as TData;

            if (data == null)
                return false;

            return true;
        }

        public void AddBehaviour<TBehaviour>(TBehaviour behaviour) where TBehaviour : IActorBehaviour
        {
            if (_behaviours.Contains(behaviour))
                return;

            _behaviours.Add(behaviour);
            behaviour.Owner = this;

            var interfaces = behaviour.GetType().GetInterfaces();

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

            if (behaviour is ITickable tickable)
                _tickables.Add(tickable);

            if (behaviour is IFixedTickable fixedTickable)
                _fixedTickables.Add(fixedTickable);

            if (behaviour is ILateTickable lateTickable)
                _lateTickables.Add(lateTickable);
        }

        public bool HasBehaviour<TBehaviour>() where TBehaviour : IActorBehaviour =>
            _behaviours.OfType<TBehaviour>().Any();

        public void RemoveBehaviour<TBehaviour>() where TBehaviour : IActorBehaviour
        {
            var type = typeof(TBehaviour);
            var behaviorsToRemove = _behaviours.FindAll(b => b.GetType() == type);

            foreach (var behavior in behaviorsToRemove)
            {
                _behaviours.Remove(behavior);
                behavior.Owner = null;
            }

            foreach (var listeners in _listenersMap.Values)
                listeners.RemoveAll(b => b.GetType() == type);

            _tickables.RemoveAll(t => t.GetType() == type);
            _fixedTickables.RemoveAll(f => f.GetType() == type);
            _lateTickables.RemoveAll(l => l.GetType() == type);
        }

        public void SendCommand<TCommand>(TCommand command = default) where TCommand : struct, IActorCommand
        {
            var commandType = typeof(TCommand);

            if (_listenersMap.TryGetValue(commandType, out var listeners))
            {
                for (int i = 0; i < listeners.Count; i++)
                {
                    if (listeners[i] is IActorCommandListener<TCommand> commandListener)
                        commandListener.ReactActorCommand(command);
                }
            }
        }

        public void ChangeAliveState(bool isAlive)
        {
            if (IsAlive == isAlive)
                return;

            IsAlive = isAlive;
            AliveStateChanged.Invoke();
        }

        public void Destroy()
        {
            CleanUp();

            Destroyed.Invoke();

            _isDestroyed = true;

            Destroy(gameObject);
        }

        private void FetchModules()
        {
            var modules = GetComponentsInChildren<IActorModule>(true);

            foreach (var module in modules)
            {
                if (module is IActorData actorData)
                    _data.Add(actorData);

                if (module is IActorBehaviour actorBehaviour)
                    AddBehaviour(actorBehaviour);
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

            foreach (var behaviour in _behaviours)
                behaviour.CleanUp();

            foreach (var data in _data)
                data.CleanUp();
        }
    }
}
