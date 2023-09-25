namespace abc.unity.Core
{
    public interface IComponentsProvider
    {
        TComponent GetComponent<TComponent>() where TComponent : IComponent;
    }
}