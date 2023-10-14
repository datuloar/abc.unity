using abc.unity.Common;
using abc.unity.Core;

namespace abc.unity.Example
{
    public class PlayerController : ITickable
    {
        private readonly IActor _actor;
        private readonly IInputService _input;

        public PlayerController(IActor actor, IInputService input)
        {
            _actor = actor;
            _input = input;
        }

        public void Tick(float deltaTime)
        {
            if (!_input.IsEnable)
                return;

            if (_input.IsJumpButtonClicked)
                _actor.SendCommand<JumpCommand>();
        }
    }
}