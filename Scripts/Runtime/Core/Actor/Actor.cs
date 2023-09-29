using abc.unity.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace abc.unity.Core
{
    public abstract class Actor : MonoBehaviour, IActor
    {
        private List<KeyValuePair<Type, ITickable>> _tickables = new(20);
        private List<KeyValuePair<Type, ICommandListener>> _commandListeners = new(20);
        private List<KeyValuePair<Type, IComponent>> _components = new(20);
        private List<KeyValuePair<Type, IDisposable>> _disposables = new(20);
        private List<KeyValuePair<Type, IBehaviour>> _behaviours = new(20);

        public event Action<Actor> Destroyed;

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _tickables.Count; i++)
                _tickables[i].Value.Tick(deltaTime);
        }

        public bool HasComponent<TComponent>() where TComponent : IComponent
        {
            var type = typeof(TComponent);

            foreach (var component in _components)
            {
                if (component.Key == type)
                    return true;
            }

            return false;
        }

        public void AddComponent<TComponent>(TComponent component) where TComponent : IComponent
        {
            if (HasComponent<TComponent>())
                return;

            var type = typeof(TComponent);

            _components.Add(new KeyValuePair<Type, IComponent>() { Key = type, Value = component });
        }

        public void RemoveComponent<TComponent>() where TComponent : IComponent
        {
            var type = typeof(TComponent);

            foreach (var component in _components)
            {
                if (component.Key == type)
                    _components.Remove(component);
            }
        }

        public new TComponent GetComponent<TComponent>() where TComponent : class, IComponent
        {
            if (!HasComponent<TComponent>())
                throw new InvalidOperationException($"Actor-{gameObject.name} does not have a component of type {typeof(TComponent).Name}");

            var type = typeof(TComponent);

            foreach (var component in _components)
            {
                if (component.Key == type)
                    return component.Value as TComponent;
            }

            throw new NullReferenceException();
        }

        public void AddBehaviour<TBehaviour>(TBehaviour behaviour) where TBehaviour : IBehaviour
        {
            if (HasBehaviour<TBehaviour>())
                return;

            var type = typeof(TBehaviour);

            if (behaviour is ICommandListener commandListener)
                _commandListeners.Add(new KeyValuePair<Type, ICommandListener>() { Key = type, Value = commandListener });

            if (behaviour is ITickable updatable)
                _tickables.Add(new KeyValuePair<Type, ITickable>() { Key = type, Value = updatable });

            if (behaviour is IDisposable disposable)
                _disposables.Add(new KeyValuePair<Type, IDisposable>() { Key = type, Value = disposable });

            _behaviours.Add(new KeyValuePair<Type, IBehaviour>() { Key = type, Value = behaviour });

            behaviour.Actor = this;
        }

        public bool HasBehaviour<TBehaviour>() where TBehaviour : IBehaviour
        {
            var type = typeof(TBehaviour);

            foreach (var behaviour in _behaviours)
            {
                if (behaviour.Key == type)
                    return true;
            }

            return false;
        }

        public void RemoveBehaviour<TBehaviour>() where TBehaviour : IBehaviour
        {
            var type = typeof(TBehaviour);

            foreach (var commandListener in _commandListeners)
            {
                if (commandListener.Key == type)
                    _commandListeners.Remove(commandListener);
            }

            foreach (var tickable in _tickables)
            {
                if (tickable.Key == type)
                    _tickables.Remove(tickable);
            }

            foreach (var behaviour in _behaviours)
            {
                if (behaviour.Key == type)
                    _behaviours.Remove(behaviour);
            }

            foreach (var disposable in _disposables)
            {
                if (disposable.Key == type)
                    _disposables.Remove(disposable);
            }
        }

        public void ReactCommand(ICommand command)
        {
            foreach (var commandListener in _commandListeners)
                commandListener.Value.ReactCommand(command);
        }

        public void Destroy()
        {
            Destroyed?.Invoke(this);

            foreach (var disposable in _disposables)
                disposable.Value.Dispose();

            Destroy(gameObject);
        }
    }

    public struct KeyValuePair<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;
    }
}