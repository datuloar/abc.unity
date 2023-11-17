using UnityEngine;

namespace abc.unity.Core
{
        public abstract class BaseComponent<TComponent> : MonoBehaviour, IComponent where TComponent : IComponent
    {
        private void Awake()
        {
            if (!TryGetComponent<IComponentsHolder>(out var componentHolder))
                throw new System.NullReferenceException("Missing ComponentHolder on object" + gameObject.name);

            if (componentHolder.HasComponent<TComponent>())
            {
                Destroy(this);
                return;
            }

            componentHolder.AddComponent(this);
        }
    }
}