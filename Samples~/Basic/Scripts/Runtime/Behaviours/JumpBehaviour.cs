using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.Basic
{
    public sealed class JumpBehaviour : MonoBehaviour, IActorBehaviour, IActorCommandListener<JumpCommand>
    {
        private JumpData _jump;
        private RigidbodyPhysicsData _physics;

        public IActor Owner { get; set; }

        public void Initialize()
        {
            _jump = Owner.GetData<JumpData>();
            _physics = Owner.GetData<RigidbodyPhysicsData>();
        }

        public void ReactActorCommand(JumpCommand command)
        {
            var force = new Vector3(0, _jump.Force);
            _physics.AddForce(force);
        }
    }
}
