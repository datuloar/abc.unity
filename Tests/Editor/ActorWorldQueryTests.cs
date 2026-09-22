using System;
using System.Collections.Generic;

using NUnit.Framework;

using Abc.Unity;

namespace Abc.Unity.Tests
{
    public sealed class ActorWorldQueryTests
    {
        private struct CountTrackingAction : IActorQueryAction<TrackingData>
        {
            public int Count;

            public void Execute(ActorModel actor, TrackingData data) => Count++;
        }

        private struct CountPairAction : IActorQueryAction<TrackingData, SecondaryData>
        {
            public int Count;

            public void Execute(ActorModel actor, TrackingData data1, SecondaryData data2) => Count++;
        }

        private struct CountTripleAction : IActorQueryAction<TrackingData, SecondaryData, TertiaryData>
        {
            public int Count;

            public void Execute(ActorModel actor, TrackingData data1, SecondaryData data2, TertiaryData data3) => Count++;
        }

        private struct MutatingAction : IActorQueryAction<TrackingData>
        {
            public ActorWorld World;
            public int Count;
            public ActorModel Added;
            private bool _mutated;

            public void Execute(ActorModel actor, TrackingData data)
            {
                Count++;

                if (_mutated)
                    return;

                _mutated = true;
                actor.Destroy();
                Added = new ActorModel("Added").WithData(new TrackingData());
                World.Add(Added);
            }
        }

        private struct ClearWorldAction : IActorQueryAction<TrackingData>
        {
            public ActorWorld World;
            public int Count;

            public void Execute(ActorModel actor, TrackingData data)
            {
                Count++;
                World.Clear();
            }
        }

        [Test]
        public void QueryTracksRuntimeComposition()
        {
            using var world = new ActorWorld();
            var actor = world.Add(new ActorModel());
            var query = world.Query<TrackingData>();
            var action = new CountTrackingAction();

            Assert.That(query.CandidateCount, Is.Zero);

            actor.AddData(new TrackingData());
            Assert.That(query.For(ref action), Is.EqualTo(1));
            Assert.That(action.Count, Is.EqualTo(1));

            actor.RemoveData<TrackingData>();
            Assert.That(query.CandidateCount, Is.Zero);
            Assert.That(query.For(ref action), Is.Zero);
        }

        [Test]
        public void QueryUsesSmallestExactTypeIntersection()
        {
            using var world = new ActorWorld();
            world.Add(new ActorModel().WithData(new TrackingData()).WithData(new SecondaryData()));
            world.Add(new ActorModel().WithData(new TrackingData()));
            world.Add(new ActorModel().WithData(new SecondaryData()));
            var query = world.Query<TrackingData, SecondaryData>();
            var action = new CountPairAction();

            var matched = query.For(ref action);

            Assert.That(query.CandidateCount, Is.EqualTo(2));
            Assert.That(matched, Is.EqualTo(1));
            Assert.That(action.Count, Is.EqualTo(1));
        }

        [Test]
        public void ThreeTypeQueryReturnsOnlyCompleteComposition()
        {
            using var world = new ActorWorld();
            world.Add(new ActorModel()
                .WithData(new TrackingData())
                .WithData(new SecondaryData())
                .WithData(new TertiaryData()));
            world.Add(new ActorModel().WithData(new TrackingData()).WithData(new SecondaryData()));
            var query = world.Query<TrackingData, SecondaryData, TertiaryData>();
            var action = new CountTripleAction();

            var matched = query.For(ref action);

            Assert.That(matched, Is.EqualTo(1));
            Assert.That(action.Count, Is.EqualTo(1));
        }

        [Test]
        public void QueryDefersSpawnAndKeepsRemovalStable()
        {
            using var world = new ActorWorld();
            world.Add(new ActorModel("First").WithData(new TrackingData()));
            world.Add(new ActorModel("Second").WithData(new TrackingData()));
            world.Add(new ActorModel("Third").WithData(new TrackingData()));
            var query = world.Query<TrackingData>();
            var action = new MutatingAction { World = world };

            var firstPass = query.For(ref action);

            Assert.That(firstPass, Is.EqualTo(3));
            Assert.That(action.Count, Is.EqualTo(3));
            Assert.That(world.Count, Is.EqualTo(3));
            Assert.That(query.CandidateCount, Is.EqualTo(3));

            var secondAction = new CountTrackingAction();
            Assert.That(query.For(ref secondAction), Is.EqualTo(3));
            Assert.That(secondAction.Count, Is.EqualTo(3));
            Assert.That(world.Contains(action.Added), Is.True);
        }

        [Test]
        public void QueryRemainsValidAfterWorldSwapBackRemoval()
        {
            using var world = new ActorWorld();
            var first = world.Add(new ActorModel("First").WithData(new TrackingData()));
            var second = world.Add(new ActorModel("Second").WithData(new TrackingData()));
            var third = world.Add(new ActorModel("Third").WithData(new TrackingData()));
            var query = world.Query<TrackingData>();

            world.Remove(second);
            world.Remove(first);
            var action = new CountTrackingAction();

            Assert.That(query.For(ref action), Is.EqualTo(1));
            Assert.That(action.Count, Is.EqualTo(1));
            Assert.That(world.Contains(third), Is.True);

            first.Dispose();
            second.Dispose();
        }

        [Test]
        public void QueryFiltersByTagAndAliveState()
        {
            using var world = new ActorWorld();
            world.Add(new ActorModel("Enemy", ActorTag.Enemy).WithData(new TrackingData()));
            var inactive = world.Add(new ActorModel("Inactive Enemy", ActorTag.Enemy).WithData(new TrackingData()));
            world.Add(new ActorModel("Player", ActorTag.Player).WithData(new TrackingData()));
            inactive.Kill();
            var action = new CountTrackingAction();

            var matched = world.Query<TrackingData>()
                .WithTag(ActorTag.Enemy)
                .OnlyAlive()
                .For(ref action);

            Assert.That(matched, Is.EqualTo(1));
            Assert.That(action.Count, Is.EqualTo(1));
        }

        [Test]
        public void QueryRejectsInterfaceDataTypes()
        {
            using var world = new ActorWorld();
            Assert.Throws<NotSupportedException>(() => world.Query<ITestDataView>());
        }

        [Test]
        public void DelegateQueryProvidesPrototypeFriendlyApi()
        {
            using var world = new ActorWorld();
            world.Add(new ActorModel().WithData(new TrackingData()));
            var count = 0;

            var matched = world.Query<TrackingData>().For((actor, data) => count++);

            Assert.That(matched, Is.EqualTo(1));
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void QuerySurvivesWorldClearDuringIteration()
        {
            using var world = new ActorWorld();
            world.Add(new ActorModel().WithData(new TrackingData()));
            world.Add(new ActorModel().WithData(new TrackingData()));
            var query = world.Query<TrackingData>();
            var action = new ClearWorldAction { World = world };

            var matched = query.For(ref action);

            Assert.That(matched, Is.EqualTo(1));
            Assert.That(action.Count, Is.EqualTo(1));
            Assert.That(world.Count, Is.Zero);
            Assert.That(query.CandidateCount, Is.Zero);
        }

        [Test]
        public void RandomizedCompositionKeepsQueryIndexConsistent()
        {
            const int operationCount = 500;
            using var world = new ActorWorld();
            var actors = new List<ActorModel>();
            var random = new Random(173);
            var query = world.Query<TrackingData>();

            for (var operation = 0; operation < operationCount; operation++)
            {
                var choice = actors.Count == 0 ? 0 : random.Next(4);

                if (choice == 0)
                {
                    var actor = new ActorModel($"Actor {operation}");
                    if (random.Next(2) == 0)
                        actor.AddData(new TrackingData());

                    actors.Add(world.Add(actor));
                }
                else
                {
                    var index = random.Next(actors.Count);
                    var actor = actors[index];

                    if (choice == 1)
                    {
                        if (actor.HasData<TrackingData>())
                            actor.RemoveData<TrackingData>();
                        else
                            actor.AddData(new TrackingData());
                    }
                    else if (choice == 2)
                    {
                        world.Remove(actor);
                        actor.Dispose();
                        actors.RemoveAt(index);
                    }
                    else
                    {
                        actor.SetTag(random.Next(2) == 0 ? ActorTag.Player : ActorTag.Enemy);
                    }
                }

                var expected = 0;
                for (var i = 0; i < actors.Count; i++)
                {
                    if (actors[i].HasData<TrackingData>())
                        expected++;
                }

                var action = new CountTrackingAction();
                Assert.That(query.CandidateCount, Is.EqualTo(expected));
                Assert.That(query.For(ref action), Is.EqualTo(expected));
                Assert.That(action.Count, Is.EqualTo(expected));
            }
        }
    }
}
