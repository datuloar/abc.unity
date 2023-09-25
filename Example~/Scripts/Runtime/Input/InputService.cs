using UnityEngine;

namespace abc.unity.ExampleMonobehaviour
{
    public class InputService : MonoBehaviour
    {
        public bool IsEnable { get; set; } = true;
        public bool IsJumpButtonClicked => Input.GetMouseButtonDown(0);
    }
}