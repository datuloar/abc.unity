using UnityEngine;

namespace abc.unity.Example
{
    public class StandaloneInputService : IInputService
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