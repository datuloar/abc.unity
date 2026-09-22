using UnityEngine;

namespace Abc.Unity.Samples.Basic
{
    public sealed class StandaloneInputService : IInputService
    {
        public bool IsEnable { get; set; } = true;
        public bool IsJumpButtonClicked => Input.GetMouseButtonDown(0);
    }

    public interface IInputService
    {
        bool IsJumpButtonClicked { get; }
        bool IsEnable { get; set; }
    }
}
