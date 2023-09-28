using abc.unity.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace abc.unity.Core
{
    public abstract class Actor : MonoBehaviour, IActor
    {
        private List<ITickable> _tickables = new(20);
        private List<IDisposable> _disposables = new(20);
        private List<ICommandListener> _commandListeners = new(20);
        private List<IComponent> _components = new(20);
        private List<IBehaviour> _behaviours = new(20);

        public event Action<Actor> Dead;

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _tickables.Count; i++)
                _tickables[i].Tick(deltaTime);
        }

        public bool HasComponent<TComponent>() where TComponent : IComponent =>
            _components.OfType<TComponent>().FirstOrDefault() != null;

        public void AddComponent<TComponent>(TComponent component) where TComponent : IComponent
        {
            if (HasComponent<TComponent>())
                return;

            _components.Add(component);
        }

        public new TComponent GetComponent<TComponent>() where TComponent : class, IComponent
        {
            if (!HasComponent<TComponent>())
                throw new InvalidOperationException($"Actor-{gameObject.name} does not have a component of type {typeof(TComponent).Name}");

            return _components.OfType<TComponent>().First();
        }

        public void AddBehaviour<TBehaviour>(TBehaviour behaviour) where TBehaviour : IBehaviour
        {
            if (HasBehaviour<TBehaviour>())
                return;

            if (behaviour is ICommandListener commandListener)
                _commandListeners.Add(commandListener);

            if (behaviour is ITickable updatable)
                _tickables.Add(updatable);

            if (behaviour is IDisposable disposable)
                _disposables.Add(disposable);

            behaviour.Actor = this;
            _behaviours.Add(behaviour);
        }

        public bool HasBehaviour<TBehaviour>() where TBehaviour : IBehaviour =>
            _behaviours.OfType<TBehaviour>().FirstOrDefault() != null;

        public void ReactCommand(ICommand command)
        {
            foreach (var commandListener in _commandListeners)
                commandListener.ReactCommand(command);
        }

        public void Dispose()
        {
            Dead?.Invoke(this);

            foreach (var disposable in _disposables)
                disposable.Dispose();

            Destroy(gameObject);
        }
    }
}