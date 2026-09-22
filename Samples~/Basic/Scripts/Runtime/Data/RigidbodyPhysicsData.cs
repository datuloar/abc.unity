using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.Basic
{
    public sealed class RigidbodyPhysicsData : MonoBehaviour, IActorData
    {
        [SerializeField] private Rigidbody _rigidbody;

        public void PreInitialize()
        {
            if (_rigidbody == null && !TryGetComponent(out _rigidbody))
                throw new MissingComponentException($"{name} requires a {nameof(Rigidbody)} component.");
        }

        public void AddForce(Vector3 force) => _rigidbody.AddForce(force);
    }
}
