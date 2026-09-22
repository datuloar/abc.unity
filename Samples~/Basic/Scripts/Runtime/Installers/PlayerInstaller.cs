using System;

using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.Basic
{
    public sealed class PlayerInstaller : MonoBehaviour
    {
        [SerializeField] private Actor _actor;

        private PlayerController _playerController;

        private void Awake()
        {
            if (_actor == null)
                throw new InvalidOperationException($"{nameof(PlayerInstaller)} on {name} requires an actor reference.");

            var input = new StandaloneInputService();
            _playerController = new PlayerController(input);
            _actor.AddBehaviour(_playerController);
        }

        private void OnDestroy()
        {
            if (_actor != null && _playerController != null && _actor.HasBehaviour<PlayerController>())
                _actor.RemoveBehaviour<PlayerController>();
        }
    }
}
