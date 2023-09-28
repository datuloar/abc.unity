using UnityEngine;

namespace abc.unity.Core
{
    public abstract class ComponentBlueprint : ScriptableObject
    {
        public abstract void Add(IActor actor);
    }
}