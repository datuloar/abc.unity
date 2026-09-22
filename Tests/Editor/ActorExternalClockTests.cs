using System.Reflection;

using NUnit.Framework;
using UnityEngine;

namespace Abc.Unity.Tests
{
    public sealed class ActorExternalClockTests
    {
        private sealed class PhaseBehaviour : IActorBehaviour, IActorTick, IActorFixedTick, IActorLateTick
        {
            public IActor Owner { get; set; }
            public int Count { get; private set; }

            public void Tick(float deltaTime) => Count++;
            public void FixedTick(float fixedDeltaTime) => Count++;
            public void LateTick(float deltaTime) => Count++;
        }

        [Test]
        public void RunnerManualModeDisablesEveryAutomaticPhaseAndKeepsManualAccess()
        {
            var gameObject = new GameObject("Manual Runner");
            try
            {
                var runner = gameObject.AddComponent<ActorWorldRunner>();
                var world = runner.GetOrCreateWorld();
                var behaviour = new PhaseBehaviour();
                world.Add(new ActorModel().WithBehaviour(behaviour));
                runner.AutomaticUpdates = false;
                SendPhases(runner);
                Assert.That(behaviour.Count, Is.Zero);
                world.FixedTick(0.02f);
                Assert.That(behaviour.Count, Is.EqualTo(1));
                runner.AutomaticUpdates = true;
                SendPhases(runner);
                Assert.That(behaviour.Count, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SceneActorCanSwitchClocksWithoutDuplicateScheduling()
        {
            var gameObject = new GameObject("Manual Actor");
            try
            {
                var actor = gameObject.AddComponent<Actor>();
                var behaviour = new PhaseBehaviour();
                actor.AddBehaviour(behaviour);
                actor.Initialize();
                var scheduler = Resources.FindObjectsOfTypeAll<ActorUpdateScheduler>()[0];
                Assert.That(scheduler, Is.Not.Null);
                SendPhases(scheduler);
                Assert.That(behaviour.Count, Is.EqualTo(3));
                actor.AutomaticUpdates = false;
                SendPhases(scheduler);
                actor.FixedTick(0.02f);
                Assert.That(behaviour.Count, Is.EqualTo(4));
                actor.Kill();
                actor.Revive();
                SendPhases(scheduler);
                Assert.That(behaviour.Count, Is.EqualTo(4));
                actor.AutomaticUpdates = true;
                actor.AutomaticUpdates = true;
                SendPhases(scheduler);
                Assert.That(behaviour.Count, Is.EqualTo(7));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                foreach (var scheduler in Resources.FindObjectsOfTypeAll<ActorUpdateScheduler>())
                    Object.DestroyImmediate(scheduler.gameObject);
            }
        }

        private static void SendPhases(MonoBehaviour target)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            target.GetType().GetMethod("Update", flags).Invoke(target, null);
            target.GetType().GetMethod("FixedUpdate", flags).Invoke(target, null);
            target.GetType().GetMethod("LateUpdate", flags).Invoke(target, null);
        }
    }
}
