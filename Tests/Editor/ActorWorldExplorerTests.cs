using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

using Abc.Unity;
using Abc.Unity.Editor;

namespace Abc.Unity.Tests
{
    public sealed class ActorWorldExplorerTests
    {
        private sealed class PropertyData : IActorData
        {
            public int Value { get; set; } = 42;
            public int UnsafeProperty => throw new System.InvalidOperationException();
        }

        [Test]
        public void SnapshotReadsAutoPropertyStorageWithoutInvokingGetters()
        {
            var snapshot = new StringBuilder();
            ActorWorldExplorerFields.AppendSnapshot(snapshot, new PropertyData(), false);

            Assert.That(snapshot.ToString(), Does.Contain("Value: 42"));
            Assert.That(snapshot.ToString(), Does.Not.Contain("Unsafe Property"));
        }

        [Test]
        public void WorldSnapshotIncludesPendingModelsAndExcludesRemovedModels()
        {
            using var world = new ActorWorld("Mutation");
            using var removed = new ActorModel("Removed");
            var added = new ActorModel("Added");
            var snapshot = new List<ActorModel>();
            var owner = new ActorModel("Owner").WithBehaviour(new InvokeOnceOnTickBehaviour(() =>
            {
                world.Remove(removed);
                world.Add(added);
                world.CopyModelsTo(snapshot);
                Assert.That(added.IsPendingInWorld, Is.True);
            }));
            world.Add(owner);
            world.Add(removed);

            world.Tick(0.1f);

            Assert.That(snapshot, Is.EquivalentTo(new[] { owner, added }));
        }

        [Test]
        public void WorldSnapshotTracksMembershipAndClear()
        {
            using var world = new ActorWorld("Test");
            var first = world.Add(new ActorModel("First"));
            var second = world.Add(new ActorModel("Second"));
            var snapshot = new List<ActorModel>();

            world.CopyModelsTo(snapshot);
            Assert.That(snapshot, Is.EquivalentTo(new[] { first, second }));

            world.Remove(first);
            world.CopyModelsTo(snapshot);
            Assert.That(snapshot, Is.EqualTo(new[] { second }));

            world.Clear();
            world.CopyModelsTo(snapshot);
            Assert.That(snapshot, Is.Empty);
            first.Destroy();
        }

        [Test]
        public void ModuleSnapshotReflectsCompositionChanges()
        {
            using var model = new ActorModel("Enemy");
            var data = new TrackingData();
            var behaviour = new CountingBehaviour();
            var modules = new List<IActorModule>();

            model.AddData(data);
            model.AddBehaviour(behaviour);
            model.CopyModulesTo(modules);
            Assert.That(modules, Is.EqualTo(new IActorModule[] { data, behaviour }));

            model.RemoveData<TrackingData>();
            model.CopyModulesTo(modules);
            Assert.That(modules, Is.EqualTo(new IActorModule[] { behaviour }));
        }

        [Test]
        public void SearchCombinesScopedTermsCaseInsensitively()
        {
            using var enemy = new ActorModel("Scout", ActorTag.Enemy);
            using var player = new ActorModel("Scout", ActorTag.Player);
            enemy.AddData(new TrackingData());
            player.AddData(new SecondaryData());
            var search = new ActorWorldExplorerSearch();
            var modules = new List<IActorModule>();

            search.SetQuery("name:scout tag:ENEMY data:tracking");
            Assert.That(search.RequiresModules, Is.True);
            enemy.CopyModulesTo(modules);
            Assert.That(search.Matches(enemy, modules), Is.True);
            player.CopyModulesTo(modules);
            Assert.That(search.Matches(player, modules), Is.False);

            search.SetQuery("tag:enemy");
            Assert.That(search.RequiresModules, Is.False);
            Assert.That(search.Matches(enemy, null), Is.True);
            Assert.That(search.Matches(player, null), Is.False);
            search.SetQuery("tag:2");
            Assert.That(search.Matches(enemy, null), Is.True);
        }

        [Test]
        public void SearchMatchesBehaviourAndUnscopedTerms()
        {
            using var model = new ActorModel("Runner");
            model.AddBehaviour(new CountingBehaviour());
            var modules = new List<IActorModule>();
            model.CopyModulesTo(modules);
            var search = new ActorWorldExplorerSearch();

            search.SetQuery("behavior:counting");
            Assert.That(search.Matches(model, modules), Is.True);
            search.SetQuery("counting runner");
            Assert.That(search.Matches(model, modules), Is.True);
            search.SetQuery("data:counting");
            Assert.That(search.Matches(model, modules), Is.False);
        }
    }
}
