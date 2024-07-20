using abc.unity.Core;

namespace abc.unity.Example
{
    public interface IJumpData : IActorData
    {
        float Force { get; }
    }
}