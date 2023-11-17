using abc.unity.Core;
using UnityEngine;

namespace abc.unity.Example
{
    public class RigidbodyComponent : BaseComponent<IRigidbodyComponent>, IRigidbodyComponent
    {
        [field: SerializeField] public Rigidbody Value { get; private set; }
    }
}