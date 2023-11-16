using abc.unity.Core;
using UnityEngine;

namespace abc.unity.Example
{
    public interface IRigidbodyComponent : IComponent
    {
        Rigidbody Value { get; }
    }
}