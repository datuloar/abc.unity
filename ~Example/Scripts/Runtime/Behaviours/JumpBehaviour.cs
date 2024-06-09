using abc.unity.Core;
using UnityEngine;

namespace abc.unity.Example
{
    public class JumpBehaviour : MonoBehaviour, IJumpBehaviour
    {
        public IActor Owner { get; set; }

        public void ReactActorCommand(JumpCommand command)
        {
            if (!Owner.TryGetData<IJumpData>(out var jumpData))
                return;

            if (!Owner.TryGetData<IPhysicsData>(out var physicsData))
                return;

            var force = new Vector3(0, jumpData.Force);
            physicsData.AddForce(force);
        }
    }
}