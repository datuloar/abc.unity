using abc.unity.Core;

namespace abc.unity.Example
{
    public interface IJumpBehaviour : IActorBehaviour, IActorCommandListener<JumpCommand>
    {

    }
}