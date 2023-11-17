using abc.unity.Common;
using System;
using UnityEngine;

namespace abc.unity.Core
{
    public abstract class BaseBehaviour<TBehaviour> : MonoBehaviour, IInitializable, IDisposable, IBehaviour where TBehaviour : IBehaviour
    {
        public IActor Actor { get; set; }

        protected abstract TBehaviour _behaviour { get; }

        private void Awake()
        {
            if (!TryGetComponent<IBehavioursHolder>(out var behaviourReceiver))
                throw new System.NullReferenceException("Missing BehavioursHolder on object" + gameObject.name);

            if (behaviourReceiver.HasBehaviour<TBehaviour>())
            {
                Dispose();
                return;
            }

            if (_behaviour == null)
                throw new System.Exception("Behaviour is null" + gameObject.name);

            behaviourReceiver.AddBehaviour(_behaviour);
        }

        private void Start() => Initialize();

        public virtual void Initialize() { }

        public void RemoveSelf() => Actor.RemoveBehaviour<TBehaviour>();

        public void Dispose()
        {
            OnDispose();
            Destroy(this);
        }

        protected virtual void OnDispose() { }
    }
}