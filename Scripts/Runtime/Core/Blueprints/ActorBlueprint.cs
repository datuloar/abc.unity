using System.Collections.Generic;
using UnityEngine;

namespace abc.unity.Core
{
    [CreateAssetMenu(fileName = "New Actor Blueprint", menuName = "ABC/Blueprints/Actor", order = 51)]
    public class ActorBlueprint : ScriptableObject
    {
        [SerializeReference] private List<ActorDataProviderBase> _data;
        [SerializeReference] private List<ActorBehaviourProviderBase> _behaviours;

        public IEnumerable<ActorDataProviderBase> Data => _data;
        public IEnumerable<ActorBehaviourProviderBase> Behaviour => _behaviours;
    }
}