using UnityEngine;

namespace abc.unity.Core
{
    public abstract class BaseBehaviour<TBehaviour> : MonoBehaviour, IInitializable where TBehaviour : IBehaviour
    {
        protected abstract TBehaviour _behaviour { get; }

        private void Start()
        {
            if (!TryGetComponent<IBehaviourReceiver>(out var behaviourReceiver))
                throw new System.NullReferenceException("Missing BehaviourReceiver on object" + gameObject.name);

            if (_behaviour == null)
                throw new System.Exception("Behaviour is null" + gameObject.name);

            Initialize();

            behaviourReceiver.AddBehaviour(_behaviour);
        }

        public abstract void Initialize();
    }
}