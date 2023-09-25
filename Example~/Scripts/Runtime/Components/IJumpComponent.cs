using abc.unity.Core;

namespace abc.unity.ExampleMonobehaviour
{
    public interface IJumpComponent : IComponent
    {
        float Force { get; }
    }
}