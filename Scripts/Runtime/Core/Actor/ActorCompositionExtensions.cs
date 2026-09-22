using System;

namespace Abc.Unity
{
    public static class ActorCompositionExtensions
    {
        public static TActor WithBlueprint<TActor>(this TActor actor, ActorBlueprint blueprint)
            where TActor : class, IActor
        {
            EnsureActor(actor);
            actor.AddBlueprint(blueprint);
            return actor;
        }

        public static TActor WithData<TActor, TData>(this TActor actor, TData data)
            where TActor : class, IActor
            where TData : class, IActorData
        {
            EnsureActor(actor);
            actor.AddData(data);
            return actor;
        }

        public static TActor WithBehaviour<TActor, TBehaviour>(this TActor actor, TBehaviour behaviour)
            where TActor : class, IActor
            where TBehaviour : class, IActorBehaviour
        {
            EnsureActor(actor);
            actor.AddBehaviour(behaviour);
            return actor;
        }

        private static void EnsureActor<TActor>(TActor actor) where TActor : class, IActor
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
        }
    }
}
