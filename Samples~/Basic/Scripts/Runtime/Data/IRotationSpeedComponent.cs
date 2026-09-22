using Abc.Unity;

namespace Abc.Unity.Samples.Basic
{
    public interface IRotationSpeedComponent : IActorData
    {
        float Value { get; }
    }
}
