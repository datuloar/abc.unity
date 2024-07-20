using abc.unity.Core;
using UnityEngine;

namespace abc.unity.Example
{
    public interface IPhysicsData : IActorData
    {
        void AddForce(Vector3 force);
    }
}