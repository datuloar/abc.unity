using System;
using System.Collections.Generic;

using UnityEngine;

namespace Abc.Unity
{
    public interface IActorReactProperty<T> : IReadOnlyActorReactProperty<T>
    {
        new T Value { get; set; }

        void SetValueWithoutNotify(T value);
    }

    public interface IReadOnlyActorReactProperty<T>
    {
        T Value { get; }

        event Action<T> ValueChanged;
    }

    [Serializable]
    public class ActorReactProperty<T> : IActorReactProperty<T>
    {
        private static readonly EqualityComparer<T> Comparer = EqualityComparer<T>.Default;

        [SerializeField] protected T _value;

        public ActorReactProperty(T value) => _value = value;

        public ActorReactProperty() => _value = default;

        public virtual T Value
        {
            get => _value;
            set
            {
                if (Comparer.Equals(_value, value))
                    return;

                _value = value;
                OnValueChange(value);
            }
        }

        public event Action<T> ValueChanged;

        public void SetValueWithoutNotify(T value) => _value = value;

        protected virtual void OnValueChange(T value) => ValueChanged?.Invoke(value);
    }
}
