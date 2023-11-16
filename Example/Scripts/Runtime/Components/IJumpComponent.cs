using abc.unity.Core;

namespace abc.unity.Example
{
    public interface IJumpComponent : IComponent
    {
        float Force { get; }
    }
}