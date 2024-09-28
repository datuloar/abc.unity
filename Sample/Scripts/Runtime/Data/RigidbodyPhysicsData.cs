using abc.unity.Core;
using UnityEngine;

namespace abc.unity.Example
{
    public class RigidbodyPhysicsData : MonoBehaviour, IActorData
    {
        [SerializeField] private Rigidbody _rigidbody;

        public void AddForce(Vector3 force) => _rigidbody.AddForce(force);
    }
}