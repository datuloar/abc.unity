using abc.unity.Core;
using System;
using System.Collections.Generic;

namespace abc.unity.Common
{
    public class ActorsContainer : MonoSingleton<ActorsContainer>
    {
        private readonly List<IActor> _actors = new(1000);
        private readonly List<IActor> _cachedActorsList = new(1000);

        public static event Action<IActor> Added;
        public static event Action<IActor> Removed;

        public static void Add(IActor actor)
        {
            Instance._actors.Add(actor);
            Added?.Invoke(actor);
        }

        public static bool Has(ActorTag tag)
        {
            foreach(var actor in Instance._actors)
            {
                if (actor.Tag.Value == tag)
                    return true;
            }

            return false;
        }

        public static IEnumerable<IActor> GetAll(ActorTag tag)
        {
            Instance._cachedActorsList.Clear();

            foreach (var actor in Instance._actors)
            {
                if (actor.Tag.Value == tag)
                    Instance._cachedActorsList.Add(actor);
            }

            return Instance._cachedActorsList;
        }

        public static IActor Get(ActorTag tag)
        {
            foreach (var actor in Instance._actors)
            {
                if (actor.Tag.Value == tag)
                    return actor;
            }

            throw new NullReferenceException($"Missing actor by tag {tag}");
        }

        public static void Remove(IActor removableActor)
        {
            foreach (var actor in Instance._actors)
            {
                if (actor == removableActor)
                {
                    Instance._actors.Remove(actor);
                    Removed?.Invoke(actor);
                    return;
                }
            }
        }

        public static void CleanUp() => Instance._actors.Clear();
    }
}
