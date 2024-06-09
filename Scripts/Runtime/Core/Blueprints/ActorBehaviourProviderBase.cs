using System;
using UnityEngine;

namespace abc.unity.Core
{
    public abstract class ActorBehaviourProviderBase : ScriptableObject
    {
        public abstract IActorBehaviour GetBehaviour();

        public abstract Type GetBehaviourType();
    }
}