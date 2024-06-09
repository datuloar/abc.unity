using abc.unity.Core;
using System.Threading.Tasks;
using UnityEngine;

namespace abc.unity.Example
{
    public class PlayerInstaller : MonoBehaviour
    {
        [SerializeField] private Actor _actor; // we can bind actor in zenject or somthing

        private PlayerController _playerController;

        private void Awake()
        {
            var input = new StandaloneInputService();
            _playerController = new PlayerController(_actor, input);
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            _playerController.Tick(deltaTime);
        }
    }
}