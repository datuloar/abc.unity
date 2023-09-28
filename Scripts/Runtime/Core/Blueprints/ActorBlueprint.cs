using System.Collections.Generic;
using UnityEngine;

namespace abc.unity.Core
{
    [CreateAssetMenu(fileName = "New Actor Blueprint", menuName = "ABC/Blueprints/Actor", order = 51)]
    public class ActorBlueprint : ScriptableObject
    {
        [SerializeField] private List<ComponentBlueprint> _components;
        [SerializeField] private List<BehaviourBlueprint> _behaviours;
        [SerializeField] private Actor _prefab;

        public TActor CreateActor<TActor>(Vector3 position, Quaternion rotation) where TActor : class, IActor
        {
            var instance = Instantiate(_prefab, position, rotation);

            foreach (var component in _components)
                component.Add(instance);

            foreach (var behaviour in _behaviours)
                behaviour.Add(instance);

            return instance as TActor;
        }
    }
}