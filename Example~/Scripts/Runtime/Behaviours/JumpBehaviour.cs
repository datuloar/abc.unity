using abc.unity.Core;
using UnityEngine;

namespace abc.unity.ExampleMonobehaviour
{
    [RequireComponent(typeof(JumpComponent), typeof(RigidbodyComponent))]
    public class JumpBehaviour : BaseBehaviour<IJumpBehaviour>, IJumpBehaviour
    {
        private IJumpComponent _jumpComponent;
        private IRigidbodyComponent _rigidbodyComponent;

        protected override IJumpBehaviour _behaviour => this;

        public override void Initialize()
        {
            _jumpComponent = Actor.GetComponent<IJumpComponent>();
            _rigidbodyComponent = Actor.GetComponent<IRigidbodyComponent>();
        }

        public void ReactCommand(ICommand command)
        {
            if (command is JumpCommand)
            {
                var force = new Vector3(0, _jumpComponent.Force);
                _rigidbodyComponent.Value.AddForce(force, ForceMode.Force);
            }
        }
    }
}