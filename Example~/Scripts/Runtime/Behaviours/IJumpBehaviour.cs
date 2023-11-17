using abc.unity.Common;
using abc.unity.Core;

namespace abc.unity.Example
{
    public interface IJumpBehaviour : IBehaviour, ICommandListener<JumpCommand>
    {

    }
}