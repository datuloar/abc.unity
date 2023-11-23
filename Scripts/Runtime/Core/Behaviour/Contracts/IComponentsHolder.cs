namespace abc.unity.Core
{
    public interface IComponentsHolder
    {
        void AddComponent<TComponent>(TComponent component) where TComponent : IComponent;
        TComponent GetComponent<TComponent>() where TComponent : class, IComponent;
        bool HasComponent<TComponent>() where TComponent : IComponent;
        void RemoveComponent<TComponent>() where TComponent : IComponent;
        bool TryGetComponent<TComponent>(out TComponent component) where TComponent : class, IComponent;
    }
}