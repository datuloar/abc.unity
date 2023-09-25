using abc.unity.Core;
using UnityEngine;

namespace abc.unity.ExampleMonobehaviour
{
    public interface IRigidbodyComponent : IComponent
    {
        Rigidbody Value { get; }
    }
}