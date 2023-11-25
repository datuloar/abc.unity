using abc.unity.Core;
using System.Collections.Generic;
using UnityEngine;

namespace abc.unity.Common
{
    public class ActorsUpdateManager : MonoSingleton<ActorsUpdateManager>
    {
        private List<IActor> _tickableActors = new(1000);
        private List<IActor> _fixedTickableActors = new(1000);
        private List<IActor> _lateTickableActors = new(1000);

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            for (int i = 0; i < _tickableActors.Count; i++)
                _tickableActors[i].Tick(deltaTime);
        }

        private void FixedUpdate()
        {
            var deltaTime = Time.deltaTime;

            for (int i = 0; i < _fixedTickableActors.Count; i++)
                _fixedTickableActors[i].FixedTick(deltaTime);
        }

        private void LateUpdate()
        {
            var deltaTime = Time.deltaTime;

            for (int i = 0; i < _lateTickableActors.Count; i++)
                _lateTickableActors[i].LateTick(deltaTime);
        }

        public static void AddTickable(IActor actor)
        {
            if (Instance._tickableActors.Contains(actor))
                throw new System.InvalidOperationException($"Actor {actor.transform.name} already contains in tickable");

            Instance._tickableActors.Add(actor);
        }

        public static void AddFixedTickable(IActor actor)
        {
            if (Instance._fixedTickableActors.Contains(actor))
                throw new System.InvalidOperationException($"Actor {actor.transform.name} already contains in fixedTickable");

            Instance._fixedTickableActors.Add(actor);
        }

        public static void AddLateTickable(IActor actor)
        {
            if (Instance._lateTickableActors.Contains(actor))
                throw new System.InvalidOperationException($"Actor {actor.transform.name} already contains in lateTickable");

            Instance._lateTickableActors.Add(actor);
        }

        public static void RemoveTickable(IActor actor) => Instance._tickableActors.Remove(actor);

        public static void RemoveFixedTickable(IActor actor) => Instance._fixedTickableActors.Remove(actor);

        public static void RemoveLateTickable(IActor actor) => Instance._lateTickableActors.Remove(actor);
    }
}