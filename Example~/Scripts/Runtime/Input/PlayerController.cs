using abc.unity.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace abc.unity.ExampleMonobehaviour
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private InputService _inputService;
        [SerializeField] private Actor _actor;

        private void Update()
        {
            if (!_inputService.IsEnable)
                return;

            if (_inputService.IsJumpButtonClicked)
                _actor.ReactCommand(new JumpCommand());
        }
    }
}