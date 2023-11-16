using System;
using System.Collections.Generic;

namespace abc.unity.Core
{
    public class ActorsContainer
    {
        private readonly HashSet<IActor> _actors = new(1000);
        private readonly List<IActor> _cachedActorsList = new(1000);

        private static ActorsContainer _instance;
        public static ActorsContainer Container => _instance ??= new();

        public static event Action<IActor> ActorAdded;
        public static event Action<IActor> ActorRemoved;

        public static void Add(IActor actor)
        {
            Container._actors.Add(actor);
            ActorAdded?.Invoke(actor);
        }

        public static bool Has(ActorTag tag)
        {
            foreach(var actor in Container._actors)
            {
                if (actor.Tag == tag)
                    return true;
            }

            return false;
        }

        public static IEnumerable<IActor> GetAll(ActorTag tag)
        {
            Container._cachedActorsList.Clear();

            foreach (var actor in Container._actors)
            {
                if (actor.Tag == tag)
                    Container._cachedActorsList.Add(actor);
            }

            return Container._cachedActorsList;
        }

        public static IActor Get(ActorTag tag)
        {
            foreach (var actor in Container._actors)
            {
                if (actor.Tag == tag)
                    return actor;
            }

            throw new NullReferenceException($"Missing actor by tag {tag}");
        }

        public static void Remove(IActor removableActor)
        {
            foreach (var actor in Container._actors)
            {
                if (actor == removableActor)
                {
                    Container._actors.Remove(actor);
                    ActorRemoved?.Invoke(actor);
                    return;
                }
            }
        }

        public static void CleanUp() => Container._actors.Clear();
    }
}
