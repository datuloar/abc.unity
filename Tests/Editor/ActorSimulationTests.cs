using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Abc.Unity.Tests
{
    public sealed class ActorSimulationTests
    {
        private sealed class FixedBehaviour : IActorBehaviour, IActorTick, IActorFixedTick, IActorLateTick
        {
            public IActor Owner { get; set; }
            public Action<float> OnFixedTick { get; set; }
            public int UpdateCount { get; private set; }
            public int LateCount { get; private set; }

            public void Tick(float deltaTime) => UpdateCount++;
            public void LateTick(float deltaTime) => LateCount++;
            public void FixedTick(float fixedDeltaTime) => OnFixedTick?.Invoke(fixedDeltaTime);
        }

        [Test]
        public void StepRunsFixedLogicThenPhysicsAndAdvancesOnlyAfterCompletion()
        {
            using var world = new ActorWorld();
            var order = new List<string>();
            var behaviour = new FixedBehaviour { OnFixedTick = delta => order.Add("Logic " + delta) };
            world.Add(new ActorModel().WithBehaviour(behaviour));
            ActorSimulation simulation = null;
            simulation = new ActorSimulation(world, 0.02f, delta =>
            {
                Assert.That(simulation.Tick, Is.Zero);
                order.Add("Physics " + delta);
            });
            simulation.Step();

            Assert.That(order, Is.EqualTo(new[] { "Logic " + 0.02f, "Physics " + 0.02f }));
            Assert.That(simulation.Tick, Is.EqualTo(1));
            Assert.That(behaviour.UpdateCount, Is.Zero);
            Assert.That(behaviour.LateCount, Is.Zero);
            Assert.That(simulation.IsFaulted, Is.False);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidStepIsRejected(float deltaTime)
        {
            using var world = new ActorWorld();
            Assert.Throws<ArgumentOutOfRangeException>(() => new ActorSimulation(world, deltaTime));
        }

        [Test]
        public void MissingAndDisposedWorldsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new ActorSimulation(null, 0.02f));
            var world = new ActorWorld();
            var simulation = new ActorSimulation(world, 0.02f);
            world.Dispose();
            Assert.Throws<ObjectDisposedException>(() => new ActorSimulation(world, 0.02f));
            Assert.Throws<ObjectDisposedException>(simulation.Step);
        }

        [Test]
        public void EscapingPhysicsFailureCannotReplayPartiallyAppliedStep()
        {
            using var world = new ActorWorld();
            var count = 0;
            world.Add(new ActorModel().WithBehaviour(new FixedBehaviour { OnFixedTick = _ => count++ }));
            var simulation = new ActorSimulation(world, 0.02f, _ => throw new InvalidOperationException("Physics failed"));
            Assert.Throws<InvalidOperationException>(simulation.Step);
            Assert.That(simulation.IsFaulted, Is.True);
            Assert.That(simulation.Tick, Is.Zero);
            Assert.Throws<InvalidOperationException>(simulation.Step);
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void ReentrantStepDoesNotAdvanceTheWorldAgain()
        {
            using var world = new ActorWorld();
            ActorSimulation simulation = null;
            simulation = new ActorSimulation(world, 0.02f, _ =>
                Assert.Throws<InvalidOperationException>(simulation.Step));
            simulation.Step();
            simulation.Step();
            Assert.That(simulation.Tick, Is.EqualTo(2));
            Assert.That(simulation.IsFaulted, Is.False);
        }

        [Test]
        public void WorldDisposedDuringLogicPreventsPhysics()
        {
            using var world = new ActorWorld();
            world.Add(new ActorModel().WithBehaviour(new FixedBehaviour { OnFixedTick = _ => world.Dispose() }));
            var physicsCount = 0;
            var simulation = new ActorSimulation(world, 0.02f, _ => physicsCount++);
            Assert.Throws<ObjectDisposedException>(simulation.Step);
            Assert.That(physicsCount, Is.Zero);
            Assert.That(simulation.IsFaulted, Is.True);
        }

        [Test]
        public void SeparateWorldsAdvanceIndependently()
        {
            using var first = new ActorWorld();
            using var second = new ActorWorld();
            var firstSimulation = new ActorSimulation(first, 1f / 60f);
            var secondSimulation = new ActorSimulation(second, 1f / 30f);
            firstSimulation.Step();
            firstSimulation.Step();
            secondSimulation.Step();
            Assert.That(firstSimulation.Tick, Is.EqualTo(2));
            Assert.That(secondSimulation.Tick, Is.EqualTo(1));
            Assert.That(firstSimulation.DeltaTime, Is.EqualTo(1f / 60f));
        }
    }
}
