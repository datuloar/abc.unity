using System;
using UnityEngine;

namespace abc.unity.Core
{
    public abstract class BaseBehaviourProvider : ScriptableObject
    {
        public abstract IBehaviour GetBehaviour();

        public abstract Type GetBehaviourType();
    }
}