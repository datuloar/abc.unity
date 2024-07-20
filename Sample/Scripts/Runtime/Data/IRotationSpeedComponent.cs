using abc.unity.Core;

namespace abc.unity.Example
{
    public interface IRotationSpeedComponent : IActorData
    {
        float Value { get; }
    }
}