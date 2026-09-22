using System;

using UnityEngine;

namespace Abc.Unity
{
    public abstract class AbstractActorBehaviourProvider<TBehaviour> : ActorBehaviourProviderBase
        where TBehaviour : class, IActorBehaviour, new()
    {
        [SerializeField] private TBehaviour _value = new TBehaviour();

        public override IActorBehaviour GetBehaviour()
        {
            _value ??= new TBehaviour();

            if (_value is ICloneable cloneable)
            {
                var cloned = cloneable.Clone();
                if (cloned is not TBehaviour typedClone || ReferenceEquals(typedClone, _value))
                    throw new InvalidOperationException($"{typeof(TBehaviour).FullName}.Clone() must return a new {typeof(TBehaviour).FullName} instance.");

                return typedClone;
            }

            var result = new TBehaviour();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(_value), result);
            return result;
        }

        public override Type GetBehaviourType() => typeof(TBehaviour);
    }
}
