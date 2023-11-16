using abc.unity.Core;
using UnityEngine;

namespace abc.unity.Example
{
    public class CoinsSpawner : MonoBehaviour
    {
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Actor _actorPrefab;

        private void Start()
        {
            var instance = Object.Instantiate(_actorPrefab, _spawnPoint);
            instance.Initialize();
        }
    }
}