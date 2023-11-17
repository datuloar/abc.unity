using UnityEngine;

namespace abc.unity.Core
{
    public abstract class BaseComponent<TComponent> : MonoBehaviour, IComponent where TComponent : IComponent
    {
        protected abstract TComponent _component { get; }

        private void Awake()
        {
            if (!TryGetComponent<IComponentsHolder>(out var componentHolder))
                throw new System.NullReferenceException("Missing ComponentHolder on object" + gameObject.name);

            if (componentHolder.HasComponent<TComponent>())
            {
                Destroy(this);
                return;
            }

            if (_component == null)
                throw new System.Exception("Component is null" + gameObject.name);

            componentHolder.AddComponent(_component);
        }
    }
}