using abc.unity.Common;
using UnityEngine;

namespace abc.unity.Core
{
    public abstract class BaseBehaviour<TBehaviour> : MonoBehaviour, IInitializable, IBehaviour where TBehaviour : IBehaviour
    {
        public IActor Actor { get; set; }

        protected abstract TBehaviour _behaviour { get; }

        private void Awake()
        {
            if (!TryGetComponent<IBehaviourReceiver>(out var behaviourReceiver))
                throw new System.NullReferenceException("Missing BehaviourReceiver on object" + gameObject.name);

            if (behaviourReceiver.HasBehaviour<TBehaviour>())
            {
                Destroy(this);
                return;
            }

            if (_behaviour == null)
                throw new System.Exception("Behaviour is null" + gameObject.name);

            behaviourReceiver.AddBehaviour(_behaviour);
        }

        private void Start() => Initialize();

        public abstract void Initialize();
    }
}