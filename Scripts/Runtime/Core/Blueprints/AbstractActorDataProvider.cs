using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace abc.unity.Core
{
    public abstract class AbstractActorDataProvider<TComponent> : ActorDataProviderBase where TComponent : IActorData, ICloneable
    {
        [SerializeField] private TComponent _value;

        public override IActorData GetData() => _value.Clone() as IActorData;

        public override Type GetDataType() => typeof(TComponent);
    }
}