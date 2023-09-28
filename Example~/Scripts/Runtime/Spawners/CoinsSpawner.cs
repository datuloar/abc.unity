using abc.unity.Core;
using UnityEngine;

namespace abc.unity.Example
{
    public class CoinsSpawner : MonoBehaviour
    {
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private ActorBlueprint _coinBlueprint;

        private void Start()
        {
            _coinBlueprint.CreateActor<Coin>(_spawnPoint.transform.position, Quaternion.identity);
        }
    }
}