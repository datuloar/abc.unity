using System;
using System.Collections.Generic;
using UnityEngine;

namespace abc.unity.Core
{
    public abstract class Actor : MonoBehaviour, IActor
    {
        private List<ITickable> _tickables = new(20);
        private List<IDisposable> _disposables = new(20);
        private List<ICommandListener> _commandListeners = new(20);

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _tickables.Count; i++)
                _tickables[i].Tick(deltaTime);
        }

        public void AddBehaviour<TBehaviour>(TBehaviour behaviour) where TBehaviour : IBehaviour
        {
            if (behaviour is IActorHolder actorHolder)
                actorHolder.Actor = this;

            if (behaviour is ICommandListener commandListener)
                _commandListeners.Add(commandListener);

            if (behaviour is ITickable updatable)
                _tickables.Add(updatable);

            if (behaviour is IDisposable disposable)
                _disposables.Add(disposable);
        }

        public new bool TryGetComponent<TComponent>(out TComponent component) where TComponent : IComponent
        {
            if (base.TryGetComponent(out component))
                return true;

            return false;
        }

        public void ReactCommand(ICommand command)
        {
            foreach (var commandListener in _commandListeners)
                commandListener.ReactCommand(command);
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
                disposable.Dispose();

            Destroy(gameObject);
        }
    }
}