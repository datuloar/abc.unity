using System;
using System.Collections.Generic;

namespace Abc.Unity
{
    public static class ActorRegistry
    {
        public static event Action<IActor> Added;
        public static event Action<IActor> Removed;

        public static int Count => ActorRegistryHost.Count;

        public static bool Has(ActorTag tag) => ActorRegistryHost.Has(tag);

        public static IReadOnlyList<IActor> GetAll(ActorTag tag) => ActorRegistryHost.GetAll(tag);

        public static IActor Get(ActorTag tag) => ActorRegistryHost.Get(tag);

        public static bool TryGet(ActorTag tag, out IActor actor) => ActorRegistryHost.TryGet(tag, out actor);

        internal static void Add(IActor actor) => ActorRegistryHost.Add(actor);

        internal static bool Remove(IActor actor) => ActorRegistryHost.Remove(actor);

        internal static void RefreshTag(IActor actor) => ActorRegistryHost.RefreshTag(actor);

        internal static void CleanUp() => ActorRegistryHost.CleanUp();

        internal static void InvokeAdded(IActor actor) => InvokeSafely(Added, actor);

        internal static void InvokeRemoved(IActor actor) => InvokeSafely(Removed, actor);

        internal static void ResetEvents()
        {
            Added = null;
            Removed = null;
        }

        private static void InvokeSafely(Action<IActor> callback, IActor actor)
        {
            var handlers = callback?.GetInvocationList();
            if (handlers == null)
                return;

            for (var i = 0; i < handlers.Length; i++)
            {
                try
                {
                    ((Action<IActor>)handlers[i]).Invoke(actor);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception, actor as UnityEngine.Object);
                }
            }
        }
    }
}
