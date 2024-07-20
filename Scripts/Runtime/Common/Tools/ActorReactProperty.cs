using System;
using UnityEngine;

namespace abc.unity.Common
{
    public interface IActorReactProperty<T>
    {
        T Value { get; set; }

        event Action<T> ValueChanged;
    }

    public interface IReadOnlyActorReactProperty<T>
    {
        T Value { get; }

        event Action<T> ValueChanged;
    }

    [Serializable]
    public class ActorReactProperty<T> : IActorReactProperty<T>, IReadOnlyActorReactProperty<T> where T : struct
    {
        [SerializeField] protected T _value;

        public ActorReactProperty(T value) => Value = value;

        public ActorReactProperty() => Value = default;

        public virtual T Value
        {
            get => _value;
            set
            {
                if (!_value.Equals(value))
                {
                    _value = value;
                    OnValueChange();
                }
            }
        }

        public event Action<T> ValueChanged = delegate { };

        protected virtual void OnValueChange() => ValueChanged.Invoke(Value);
    }
}