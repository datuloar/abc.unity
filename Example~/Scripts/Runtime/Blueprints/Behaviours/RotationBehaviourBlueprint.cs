using abc.unity.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace abc.unity.Example
{
    [CreateAssetMenu(fileName = nameof(RotationBehaviourBlueprint), menuName = "ABC/Blueprints/Behaviours/"+ nameof(RotationBehaviourBlueprint))]
    public class RotationBehaviourBlueprint : BehaviourBlueprint
    {
        public override void Add(IActor actor) => actor.AddBehaviour(new RotationBehaviour());
    }
}