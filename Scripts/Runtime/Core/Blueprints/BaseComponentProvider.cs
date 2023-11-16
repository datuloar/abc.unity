using System;
using UnityEngine;

namespace abc.unity.Core
{
    public abstract class BaseComponentProvider : ScriptableObject
    {
        public abstract IComponent GetComponent();

        public abstract Type GetComponentType();
    }
}