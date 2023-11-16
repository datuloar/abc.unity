using System;
using UnityEngine;

namespace abc.unity.Core
{
    public abstract class BehaviourProvider<TBehaviour> : BaseBehaviourProvider where TBehaviour : IBehaviour
    {
        [SerializeField] private TBehaviour _value;

        public override IBehaviour GetBehaviour() => _value;

        public override Type GetBehaviourType() => typeof(TBehaviour);
    }
}