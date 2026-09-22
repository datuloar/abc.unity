using System.Collections.Generic;

using UnityEngine;

namespace Abc.Unity
{
    internal static class ActorModuleCollector
    {
        public static void Collect(
            Actor actor,
            IReadOnlyList<ActorBlueprint> blueprints,
            List<MonoBehaviour> componentBuffer,
            ActorModuleStore modules)
        {
            for (var i = 0; i < blueprints.Count; i++)
                modules.AddBlueprint(blueprints[i], false);

            componentBuffer.Clear();

            try
            {
                actor.GetComponentsInChildren(true, componentBuffer);

                for (var i = 0; i < componentBuffer.Count; i++)
                {
                    var component = componentBuffer[i];
                    if (component == null || component.GetComponentInParent<Actor>(true) != actor)
                        continue;

                    if (component is IActorData data)
                        modules.AddData(data, false);

                    if (component is IActorBehaviour behaviour)
                        modules.AddBehaviour(behaviour, false);
                }
            }
            finally
            {
                componentBuffer.Clear();
            }
        }
    }
}
