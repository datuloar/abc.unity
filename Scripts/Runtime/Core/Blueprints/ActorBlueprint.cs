using System.Collections.Generic;
using System.Collections.ObjectModel;

using UnityEngine;

namespace Abc.Unity
{
    [HelpURL("https://github.com/datuloar/abc.unity#blueprints")]
    [CreateAssetMenu(fileName = "New Actor Blueprint", menuName = "ABC/Blueprints/Actor", order = 51)]
    public sealed class ActorBlueprint : ScriptableObject
    {
        [SerializeField] private List<ActorDataProviderBase> _data = new List<ActorDataProviderBase>();
        [SerializeField] private List<ActorBehaviourProviderBase> _behaviours = new List<ActorBehaviourProviderBase>();

        private ReadOnlyCollection<ActorDataProviderBase> _dataView;
        private ReadOnlyCollection<ActorBehaviourProviderBase> _behaviourView;

        public IReadOnlyList<ActorDataProviderBase> Data => _dataView ??= _data.AsReadOnly();
        public IReadOnlyList<ActorBehaviourProviderBase> Behaviours => _behaviourView ??= _behaviours.AsReadOnly();

        private void OnValidate()
        {
            _dataView = null;
            _behaviourView = null;
        }
    }
}
