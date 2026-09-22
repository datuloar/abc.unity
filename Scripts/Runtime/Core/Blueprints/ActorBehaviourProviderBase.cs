using System;

using UnityEngine;

namespace Abc.Unity
{
    public abstract class ActorBehaviourProviderBase : ScriptableObject
    {
        public abstract IActorBehaviour GetBehaviour();

        public abstract Type GetBehaviourType();
    }
}
