using System.Collections.Generic;
using UnityEngine;

namespace abc.unity.Core
{
    [CreateAssetMenu(fileName = "New Actor Blueprint", menuName = "ABC/Blueprints/Actor", order = 51)]
    public class ActorBlueprint : ScriptableObject
    {
        [SerializeReference] private List<BaseComponentProvider> _components;
        [SerializeReference] private List<BaseBehaviourProvider> _behaviours;

        public IEnumerable<BaseComponentProvider> Components => _components;
        public IEnumerable<BaseBehaviourProvider> Behaviour => _behaviours;
    }
}