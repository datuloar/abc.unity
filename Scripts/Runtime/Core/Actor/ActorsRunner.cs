using System.Collections.Generic;
using UnityEngine;

namespace abc.unity.Core
{
    public class ActorsRunner : MonoBehaviour, ICommandListener
    {
        [field: SerializeField] private List<Actor> _actors;

        private void OnEnable()
        {
            foreach (var actor in _actors)
                actor.Destroyed += OnActorDestroyed;
        }

        private void OnDisable()
        {
            foreach (var actor in _actors)
                actor.Destroyed -= OnActorDestroyed;
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            var index = _actors.Count;

            while (index-- > 0)
                _actors[index].Tick(deltaTime);
        }

        public void ReactCommand(ICommand command)
        {
            foreach (var actor in _actors)
                actor.ReactCommand(command);
        }

        public void AddActor(Actor actor) => _actors.Add(actor);

        public void RemoveActor(Actor actor) => _actors.Remove(actor);

        private void OnActorDestroyed(Actor actor) => RemoveActor(actor);
    }
}