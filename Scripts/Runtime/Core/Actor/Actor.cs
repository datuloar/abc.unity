using abc.unity.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace abc.unity.Core
{
    public abstract class Actor : MonoBehaviour, IActor
    {
        private readonly List<IFixedTickable> _fixedTickables = new(100);
        private readonly List<ITickable> _tickables = new(100);
        private readonly List<IComponent> _components = new(100);
        private readonly List<IDisposable> _disposables = new(100);
        private readonly List<IBehaviour> _behaviours = new(100);
        private readonly Dictionary<Type, List<object>> _listenersMap = new(100);

        public bool IsAlive { get; private set; } = true;

        public event Action<IActor> Destroyed;

        public void Tick(float deltaTime)
        {
            if (!IsAlive)
                return;

            for (int i = 0; i < _tickables.Count; i++)
                _tickables[i].Tick(deltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (!IsAlive)
                return;

            for (int i = 0; i < _fixedTickables.Count; i++)
                _fixedTickables[i].FixedTick(fixedDeltaTime);
        }

        public bool HasComponent<TComponent>() where TComponent : IComponent =>
            _components.Exists(c => c is TComponent);

        public void AddComponent<TComponent>(TComponent component) where TComponent : IComponent
        {
            if (!HasComponent<TComponent>())
                _components.Add(component);
        }

        public void RemoveComponent<TComponent>() where TComponent : IComponent =>
            _components.RemoveAll(c => c is TComponent);

        public new TComponent GetComponent<TComponent>() where TComponent : class, IComponent
        {
            var component = _components.Find(c => c is TComponent) as TComponent;

            if (component == null)
                throw new InvalidOperationException($"Actor-{gameObject.name} does not have a component of type {typeof(TComponent).Name}");

            return component;
        }

        public void AddBehaviour<TBehaviour>(TBehaviour behaviour) where TBehaviour : IBehaviour
        {
            if (HasBehaviour<TBehaviour>())
                return;

            _behaviours.Add(behaviour);
            behaviour.Actor = this;

            var type = typeof(TBehaviour);

            foreach (var implementedInterface in type.GetInterfaces())
            {
                if (implementedInterface.IsGenericType
                    && implementedInterface.GetGenericTypeDefinition() == typeof(ICommandListener<>))
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

            if (behaviour is IDisposable disposable)
                _disposables.Add(disposable);
        }

        public bool HasBehaviour<TBehaviour>() where TBehaviour : IBehaviour =>
            _behaviours.Exists(b => b is TBehaviour);

        public void RemoveBehaviour<TBehaviour>() where TBehaviour : IBehaviour
        {
            var type = typeof(TBehaviour);
            var behaviorsToRemove = _behaviours.FindAll(b => b.GetType() == type);

            foreach (var behavior in behaviorsToRemove)
            {
                _behaviours.Remove(behavior);
                behavior.Actor = null;
            }

            foreach (var listeners in _listenersMap.Values)
                listeners.RemoveAll(b => b.GetType() == type);

            _tickables.RemoveAll(t => t.GetType() == type);
            _disposables.RemoveAll(d => d.GetType() == type);
        }

        public void SendCommand<TCommand>(TCommand command = default) where TCommand : struct, ICommand
        {
            var commandType = typeof(TCommand);

            if (_listenersMap.TryGetValue(commandType, out var listeners))
            {
                foreach (var listener in listeners)
                {
                    if (listener is ICommandListener<TCommand> commandListener)
                        commandListener.ReactCommand(command);
                }
            }
        }

        public void SetDead(bool isDead)
        {
            if (isDead)
                SendCommand<ActorDeadCommand>();
            else
                SendCommand<ActorReviveCommand>();

            IsAlive = isDead;
        }

        public void Destroy()
        {
            Destroyed?.Invoke(this);

            foreach (var disposable in _disposables)
                disposable.Dispose();

            Destroy(gameObject);
        }
    }
}