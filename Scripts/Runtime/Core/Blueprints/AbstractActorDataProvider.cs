using System;

using UnityEngine;

namespace Abc.Unity
{
    public abstract class AbstractActorDataProvider<TData> : ActorDataProviderBase
        where TData : class, IActorData, new()
    {
        [SerializeField] private TData _value = new TData();

        public override IActorData GetData()
        {
            _value ??= new TData();

            if (_value is ICloneable cloneable)
            {
                var cloned = cloneable.Clone();
                if (cloned is not TData typedClone || ReferenceEquals(typedClone, _value))
                    throw new InvalidOperationException($"{typeof(TData).FullName}.Clone() must return a new {typeof(TData).FullName} instance.");

                return typedClone;
            }

            var result = new TData();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(_value), result);
            return result;
        }

        public override Type GetDataType() => typeof(TData);
    }
}
