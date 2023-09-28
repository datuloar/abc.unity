using abc.unity.Core;

namespace abc.unity.Example
{
    public interface IRotationSpeedComponent : IComponent
    {
        float Value { get; }
    }
}