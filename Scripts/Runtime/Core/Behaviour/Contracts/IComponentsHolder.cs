namespace abc.unity.Core
{
    public interface IComponentsHolder
    {
        void AddComponent<TComponent>(TComponent component) where TComponent : IComponent;
        TComponent GetComponent<TComponent>() where TComponent : class, IComponent;
        bool HasComponent<TComponent>() where TComponent : IComponent;
    }
}