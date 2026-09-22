using System;

using UnityEngine;

namespace Abc.Unity
{
    internal sealed class ActorUpdateScheduler : ActorServiceHost<ActorUpdateScheduler>
    {
        private readonly DeferredList<IActor> _tickableActors = new DeferredList<IActor>();
        private readonly DeferredList<IActor> _fixedTickableActors = new DeferredList<IActor>();
        private readonly DeferredList<IActor> _lateTickableActors = new DeferredList<IActor>();

        private void Update() => TickUpdate(_tickableActors, Time.deltaTime);

        private void FixedUpdate() => TickFixedUpdate(_fixedTickableActors, Time.fixedDeltaTime);

        private void LateUpdate() => TickLateUpdate(_lateTickableActors, Time.deltaTime);

        public static void AddTickable(IActor actor)
        {
            if (actor == null || actor is UnityEngine.Object unityObject && unityObject == null)
                throw new ArgumentNullException(nameof(actor));

            if (Instance._tickableActors.Contains(actor))
                throw new InvalidOperationException($"Actor {actor.Name} is already registered for Tick.");

            Instance._tickableActors.Add(actor);
        }

        public static void AddFixedTickable(IActor actor)
        {
            if (actor == null || actor is UnityEngine.Object unityObject && unityObject == null)
                throw new ArgumentNullException(nameof(actor));

            if (Instance._fixedTickableActors.Contains(actor))
                throw new InvalidOperationException($"Actor {actor.Name} is already registered for FixedTick.");

            Instance._fixedTickableActors.Add(actor);
        }

        public static void AddLateTickable(IActor actor)
        {
            if (actor == null || actor is UnityEngine.Object unityObject && unityObject == null)
                throw new ArgumentNullException(nameof(actor));

            if (Instance._lateTickableActors.Contains(actor))
                throw new InvalidOperationException($"Actor {actor.Name} is already registered for LateTick.");

            Instance._lateTickableActors.Add(actor);
        }

        public static bool RemoveTickable(IActor actor) =>
            TryGetInstance(out var instance) && instance._tickableActors.Remove(actor);

        public static bool RemoveFixedTickable(IActor actor) =>
            TryGetInstance(out var instance) && instance._fixedTickableActors.Remove(actor);

        public static bool RemoveLateTickable(IActor actor) =>
            TryGetInstance(out var instance) && instance._lateTickableActors.Remove(actor);

        public static void CleanUp()
        {
            if (TryGetInstance(out var instance))
                instance.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            if (TryGetInstance(out var instance))
                instance.Clear();

            ResetSingletonState();
        }

        protected override void OnDestroy()
        {
            Clear();
            base.OnDestroy();
        }

        private static void TickUpdate(DeferredList<IActor> actors, float deltaTime)
        {
            var count = actors.BeginIteration();

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var actor = actors[i];
                    if (actor == null || actor is UnityEngine.Object unityObject && unityObject == null)
                        continue;

                    try
                    {
                        actor.Tick(deltaTime);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, actor as UnityEngine.Object);
                    }
                }
            }
            finally
            {
                actors.EndIteration();
            }
        }

        private static void TickFixedUpdate(DeferredList<IActor> actors, float deltaTime)
        {
            var count = actors.BeginIteration();

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var actor = actors[i];
                    if (actor == null || actor is UnityEngine.Object unityObject && unityObject == null)
                        continue;

                    try
                    {
                        actor.FixedTick(deltaTime);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, actor as UnityEngine.Object);
                    }
                }
            }
            finally
            {
                actors.EndIteration();
            }
        }

        private static void TickLateUpdate(DeferredList<IActor> actors, float deltaTime)
        {
            var count = actors.BeginIteration();

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var actor = actors[i];
                    if (actor == null || actor is UnityEngine.Object unityObject && unityObject == null)
                        continue;

                    try
                    {
                        actor.LateTick(deltaTime);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, actor as UnityEngine.Object);
                    }
                }
            }
            finally
            {
                actors.EndIteration();
            }
        }

        private void Clear()
        {
            _tickableActors.Clear();
            _fixedTickableActors.Clear();
            _lateTickableActors.Clear();
        }
    }
}
