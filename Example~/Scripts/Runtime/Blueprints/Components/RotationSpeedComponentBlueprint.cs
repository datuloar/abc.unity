using abc.unity.Core;
using UnityEngine;

namespace abc.unity.Example
{
    [CreateAssetMenu(fileName = nameof(RotationSpeedComponentBlueprint), menuName = "ABC/Blueprints/Components/" + nameof(RotationSpeedComponentBlueprint))]
    public class RotationSpeedComponentBlueprint : ComponentBlueprint
    {
        [SerializeField] private RotationSpeedComponent _value;

        public override void Add(IActor actor) => actor.AddComponent(_value);
    }
}