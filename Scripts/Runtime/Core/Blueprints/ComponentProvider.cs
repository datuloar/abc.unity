using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace abc.unity.Core
{
    public abstract class ComponentProvider<TComponent> : BaseComponentProvider where TComponent : IComponent, ICloneable
    {
        [SerializeField] private TComponent _value;

        public override IComponent GetComponent() => _value.Clone() as IComponent;

        public override Type GetComponentType() => typeof(TComponent);
    }
}