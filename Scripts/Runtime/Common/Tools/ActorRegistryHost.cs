using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using UnityEngine;

namespace Abc.Unity
{
    internal sealed class ActorRegistryHost : ActorServiceHost<ActorRegistryHost>
    {
        private struct ActorRegistration
        {
            public ActorRegistration(ActorTag tag, int index, Action<ActorTag> tagHandler)
            {
                Tag = tag;
                Index = index;
                TagHandler = tagHandler;
            }

            public ActorTag Tag;
            public int Index;
            public Action<ActorTag> TagHandler;
        }

        private static readonly IReadOnlyList<IActor> EmptyActors = Array.Empty<IActor>();

        private readonly Dictionary<IActor, ActorRegistration> _registrations = new Dictionary<IActor, ActorRegistration>(ReferenceEqualityComparer<IActor>.Instance);
        private readonly Dictionary<ActorTag, List<IActor>> _actorsByTag = new Dictionary<ActorTag, List<IActor>>();
        private readonly Dictionary<ActorTag, ReadOnlyCollection<IActor>> _viewsByTag = new Dictionary<ActorTag, ReadOnlyCollection<IActor>>();

        internal static int Count => TryGetInstance(out var instance) ? instance._registrations.Count : 0;

        internal static void Add(IActor actor)
        {
            if (actor == null || actor is UnityEngine.Object unityObject && unityObject == null)
                throw new ArgumentNullException(nameof(actor));

            var instance = Instance;
            if (instance._registrations.ContainsKey(actor))
                throw new InvalidOperationException($"Actor {actor.Name} is already registered.");

            var tag = actor.Tag.Value;
            var actors = instance.GetOrCreateTagList(tag);
            Action<ActorTag> handler = newTag => instance.ChangeTag(actor, newTag);
            instance._registrations.Add(actor, new ActorRegistration(tag, actors.Count, handler));
            actors.Add(actor);
            actor.Tag.ValueChanged += handler;

            ActorRegistry.InvokeAdded(actor);
        }

        internal static bool Has(ActorTag tag) =>
            TryGetInstance(out var instance) &&
            instance._actorsByTag.TryGetValue(tag, out var actors) &&
            actors.Count > 0;

        internal static IReadOnlyList<IActor> GetAll(ActorTag tag)
        {
            if (!TryGetInstance(out var instance))
                return EmptyActors;

            return instance._viewsByTag.TryGetValue(tag, out var view) ? view : EmptyActors;
        }

        internal static IActor Get(ActorTag tag)
        {
            if (TryGet(tag, out var actor))
                return actor;

            throw new KeyNotFoundException($"No actor with tag {tag} is registered.");
        }

        internal static bool TryGet(ActorTag tag, out IActor actor)
        {
            if (TryGetInstance(out var instance) &&
                instance._actorsByTag.TryGetValue(tag, out var actors) &&
                actors.Count > 0)
            {
                actor = actors[0];
                return true;
            }

            actor = null;
            return false;
        }

        internal static bool Remove(IActor actor)
        {
            if (actor == null || !TryGetInstance(out var instance))
                return false;

            if (!instance._registrations.TryGetValue(actor, out var registration))
                return false;

            actor.Tag.ValueChanged -= registration.TagHandler;
            instance.RemoveFromTag(registration.Tag, registration.Index, actor);
            instance._registrations.Remove(actor);
            ActorRegistry.InvokeRemoved(actor);
            return true;
        }

        internal static void RefreshTag(IActor actor)
        {
            if (actor == null || actor is UnityEngine.Object unityObject && unityObject == null ||
                !TryGetInstance(out var instance) || !instance._registrations.ContainsKey(actor))
                return;

            instance.ChangeTag(actor, actor.Tag.Value);
        }

        internal static void CleanUp()
        {
            if (TryGetInstance(out var instance))
                instance.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            if (TryGetInstance(out var instance))
                instance.Clear();

            ActorRegistry.ResetEvents();
            ResetSingletonState();
        }

        protected override void OnDestroy()
        {
            Clear();
            base.OnDestroy();
        }

        private List<IActor> GetOrCreateTagList(ActorTag tag)
        {
            if (_actorsByTag.TryGetValue(tag, out var actors))
                return actors;

            actors = new List<IActor>();
            _actorsByTag.Add(tag, actors);
            _viewsByTag.Add(tag, actors.AsReadOnly());
            return actors;
        }

        private void ChangeTag(IActor actor, ActorTag newTag)
        {
            if (!_registrations.TryGetValue(actor, out var registration) || registration.Tag.Equals(newTag))
                return;

            RemoveFromTag(registration.Tag, registration.Index, actor);
            var actors = GetOrCreateTagList(newTag);
            registration.Tag = newTag;
            registration.Index = actors.Count;
            _registrations[actor] = registration;
            actors.Add(actor);
        }

        private void RemoveFromTag(ActorTag tag, int index, IActor actor)
        {
            if (!_actorsByTag.TryGetValue(tag, out var actors))
                return;

            var lastIndex = actors.Count - 1;
            if ((uint)index > (uint)lastIndex || !ReferenceEquals(actors[index], actor))
                throw new InvalidOperationException("Actor tag index is corrupted.");

            if (index != lastIndex)
            {
                var replacement = actors[lastIndex];
                actors[index] = replacement;
                var replacementRegistration = _registrations[replacement];
                replacementRegistration.Index = index;
                _registrations[replacement] = replacementRegistration;
            }

            actors.RemoveAt(lastIndex);
        }

        private void Clear()
        {
            foreach (var pair in _registrations)
            {
                if (pair.Key is UnityEngine.Object unityObject && unityObject == null)
                    continue;

                pair.Key.Tag.ValueChanged -= pair.Value.TagHandler;
            }

            _registrations.Clear();
            foreach (var actors in _actorsByTag.Values)
                actors.Clear();

            _actorsByTag.Clear();
            _viewsByTag.Clear();
        }
    }
}
