using UnityEngine;

namespace abc.unity.Core
{
    public abstract class BehaviourBlueprint : ScriptableObject
    {
        public abstract void Add(IActor actor);
    }
}