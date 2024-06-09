using System;
using UnityEngine;

namespace abc.unity.Core
{
    public abstract class AbstractActorBehaviourProvider<TBehaviour> : ActorBehaviourProviderBase where TBehaviour : IActorBehaviour
    {
        [SerializeField] private TBehaviour _value;

        public override IActorBehaviour GetBehaviour() => _value;

        public override Type GetBehaviourType() => typeof(TBehaviour);
    }
}