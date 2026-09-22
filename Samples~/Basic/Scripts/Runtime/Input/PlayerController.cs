using Abc.Unity;

namespace Abc.Unity.Samples.Basic
{
    public sealed class PlayerController : IActorBehaviour, IActorTick
    {
        private readonly IInputService _input;

        public PlayerController(IInputService input)
        {
            _input = input ?? throw new System.ArgumentNullException(nameof(input));
        }

        public IActor Owner { get; set; }

        public void Tick(float deltaTime)
        {
            if (!_input.IsEnable)
                return;

            if (_input.IsJumpButtonClicked)
                Owner.SendCommand<JumpCommand>();
        }
    }
}
