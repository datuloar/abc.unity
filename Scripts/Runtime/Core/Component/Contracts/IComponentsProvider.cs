namespace abc.unity.Core
{
    public interface IComponentsProvider
    {
        bool TryGetComponent<TComponent>(out TComponent behaviour) where TComponent : IComponent;
    }
}