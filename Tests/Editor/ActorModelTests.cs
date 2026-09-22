using System;

using NUnit.Framework;
using Unity.Profiling;

using Abc.Unity;

namespace Abc.Unity.Tests
{
    public sealed class ActorModelTests
    {
        [SetUp]
        public void SetUp()
        {
            ActorRegistry.CleanUp();
            ActorUpdateScheduler.CleanUp();
        }

        [TearDown]
        public void TearDown()
        {
            ActorRegistry.CleanUp();
            ActorUpdateScheduler.CleanUp();
        }

        [Test]
        public void ModelRunsWithoutGameObject()
        {
            using var actor = new ActorModel("Simulation Actor", ActorTag.Enemy);
            var data = new TrackingData();
            var behaviour = new CountingBehaviour();
            var composed = actor
                .WithData(data)
                .WithBehaviour(behaviour);

            actor.Initialize();
            actor.Tick(0.016f);

            Assert.That(composed, Is.SameAs(actor));
            Assert.That(actor.IsInitialized.Value, Is.True);
            Assert.That(actor.IsAlive.Value, Is.True);
            Assert.That(actor.Tag.Value, Is.EqualTo(ActorTag.Enemy));
            Assert.That(actor.GetData<TrackingData>(), Is.SameAs(data));
            Assert.That(behaviour.Owner, Is.SameAs(actor));
            Assert.That(behaviour.TickCount, Is.EqualTo(1));
        }

        [Test]
        public void WorldKeepsIterationStableWhenActorDestroysItself()
        {
            using var world = new ActorWorld();
            var first = new ActorModel("First");
            var second = new ActorModel("Second");
            var destroyer = new DestroyOwnerOnTickBehaviour();
            var counter = new CountingBehaviour();
            first.AddBehaviour(destroyer);
            second.AddBehaviour(counter);
            world.Add(first);
            world.Add(second);

            world.Tick(0.016f);

            Assert.That(destroyer.TickCount, Is.EqualTo(1));
            Assert.That(counter.TickCount, Is.EqualTo(1));
            Assert.That(world.Count, Is.EqualTo(1));
            Assert.That(world.Contains(first), Is.False);
            Assert.That(world.Contains(second), Is.True);
        }

        [Test]
        public void WorldDoesNotReactivateModelDestroyedDuringInitialization()
        {
            using var world = new ActorWorld();
            var query = world.Query<TrackingData>();
            var actor = new ActorModel("Self Destructing")
                .WithData(new TrackingData())
                .WithBehaviour(new DestroyOnInitializeBehaviour());

            Assert.DoesNotThrow(() => world.Add(actor));

            Assert.That(world.Count, Is.Zero);
            Assert.That(world.Contains(actor), Is.False);
            Assert.That(actor.IsInitialized.Value, Is.False);
            Assert.That(actor.IsAlive.Value, Is.False);
            Assert.That(query.CandidateCount, Is.Zero);
        }

        [Test]
        public void WorldDefersActorsAddedDuringTick()
        {
            using var world = new ActorWorld();
            var added = new ActorModel("Added");
            var addedCounter = new CountingBehaviour();
            added.AddBehaviour(addedCounter);
            var spawner = new ActorModel("Spawner");
            var spawnBehaviour = new InvokeOnceOnTickBehaviour(() => world.Add(added));
            spawner.AddBehaviour(spawnBehaviour);
            world.Add(spawner);

            world.Tick(0.016f);

            Assert.That(world.Count, Is.EqualTo(2));
            Assert.That(spawnBehaviour.TickCount, Is.EqualTo(1));
            Assert.That(addedCounter.TickCount, Is.Zero);

            world.Tick(0.016f);

            Assert.That(spawnBehaviour.TickCount, Is.EqualTo(2));
            Assert.That(addedCounter.TickCount, Is.EqualTo(1));
        }

        [Test]
        public void WorldRemovalUsesStableActorIndexes()
        {
            using var world = new ActorWorld();
            var first = new ActorModel("First");
            var second = new ActorModel("Second");
            var third = new ActorModel("Third");
            var firstCounter = new CountingBehaviour();
            var thirdCounter = new CountingBehaviour();
            first.AddBehaviour(firstCounter);
            third.AddBehaviour(thirdCounter);
            world.Add(first);
            world.Add(second);
            world.Add(third);

            Assert.That(world.Remove(second), Is.True);
            Assert.That(world.Remove(first), Is.True);
            world.Tick(0.016f);

            Assert.That(world.Count, Is.EqualTo(1));
            Assert.That(world.Contains(third), Is.True);
            Assert.That(firstCounter.TickCount, Is.Zero);
            Assert.That(thirdCounter.TickCount, Is.EqualTo(1));

            second.Dispose();
            first.Dispose();
        }

        [Test]
        public void ExactGenericDataLookupDoesNotAllocateAfterWarmup()
        {
            using var actor = new ActorModel();
            var data = new TrackingData();
            actor.AddData(data);
            actor.Initialize();

            for (var i = 0; i < 32; i++)
                actor.GetData<TrackingData>();

            using var recorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Internal,
                "GC.Alloc",
                1,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            TrackingData resolved = null;

            for (var i = 0; i < 100000; i++)
                resolved = actor.GetData<TrackingData>();

            recorder.Stop();
            Assert.That(resolved, Is.SameAs(data));
            Assert.That(recorder.Count, Is.Zero);
        }

        [Test]
        public void WorldTickDoesNotAllocateAfterWarmup()
        {
            using var world = new ActorWorld();

            for (var i = 0; i < 256; i++)
            {
                var actor = new ActorModel($"Actor {i}");
                actor.AddBehaviour(new AllocationBehaviour());
                world.Add(actor);
            }

            for (var i = 0; i < 32; i++)
                world.Tick(0.016f);

            using var recorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Internal,
                "GC.Alloc",
                1,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);

            for (var i = 0; i < 1024; i++)
                world.Tick(0.016f);

            recorder.Stop();
            Assert.That(recorder.Count, Is.Zero);
        }
    }
}
