using System;
using System.Collections.Generic;

using NUnit.Framework;
using UnityEngine;

namespace Abc.Unity.Tests
{
    public sealed class ActorIsolationTests
    {
        private sealed class DualRoleModule : IActorData, IActorBehaviour
        {
            public IActor Owner { get; set; }
            public int CleanUpCount { get; private set; }

            public void CleanUp() => CleanUpCount++;
        }

        private sealed class CallbackData : IActorData
        {
            public Action PreInitializing { get; set; }
            public Action Initializing { get; set; }

            public void PreInitialize() => PreInitializing?.Invoke();
            public void Initialize() => Initializing?.Invoke();
        }

        [Test]
        public void RuntimeCompositionFinishesPreInitializationBeforeNestedInitialization()
        {
            using var actor = new ActorModel();
            actor.Initialize();
            var nested = new TrackingData();
            var preInitializationFinished = false;
            var data = new CallbackData
            {
                PreInitializing = () =>
                {
                    actor.AddData(nested);
                    Assert.That(nested.InitializeCount, Is.Zero);
                    preInitializationFinished = true;
                },
                Initializing = () =>
                {
                    Assert.That(preInitializationFinished, Is.True);
                    Assert.That(nested.PreInitializeCount, Is.EqualTo(1));
                }
            };

            actor.AddData(data);

            Assert.That(nested.InitializeCount, Is.EqualTo(1));
        }

        [Test]
        public void FailedInitializationReleasesModuleOwnership()
        {
            using var first = new ActorModel();
            using var second = new ActorModel();
            var data = new CallbackData { Initializing = () => throw new InvalidOperationException() };
            first.AddData(data);

            Assert.Throws<InvalidOperationException>(() => first.Initialize());

            data.Initializing = null;
            second.AddData(data);
            Assert.DoesNotThrow(() => second.Initialize());
            Assert.That(first.HasData<CallbackData>(), Is.False);
        }

        [Test]
        public void DataCannotBelongToTwoActors()
        {
            using var first = new ActorModel();
            using var second = new ActorModel();
            var data = new TrackingData();
            first.AddData(data);

            Assert.Throws<InvalidOperationException>(() => second.AddData(data));
            Assert.That(second.HasData<TrackingData>(), Is.False);
            Assert.That(first.GetData<TrackingData>(), Is.SameAs(data));

            first.RemoveData<TrackingData>();
            Assert.DoesNotThrow(() => second.AddData(data));
        }

        [Test]
        public void ClearingBehaviourOwnerCannotBypassOwnership()
        {
            using var first = new ActorModel();
            using var second = new ActorModel();
            var behaviour = new CountingBehaviour();
            first.AddBehaviour(behaviour);
            behaviour.Owner = null;

            Assert.Throws<InvalidOperationException>(() => second.AddBehaviour(behaviour));
            Assert.That(second.HasBehaviour<CountingBehaviour>(), Is.False);
            Assert.That(first.HasBehaviour<CountingBehaviour>(), Is.True);
        }

        [Test]
        public void RemovingBehaviourRolePreservesDataOwnership()
        {
            using var actor = new ActorModel();
            using var other = new ActorModel();
            var module = new DualRoleModule();
            actor.AddData(module);
            actor.AddBehaviour(module);
            actor.Initialize();

            actor.RemoveBehaviour<DualRoleModule>();

            Assert.That(module.Owner, Is.SameAs(actor));
            Assert.That(module.CleanUpCount, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => other.AddBehaviour(module));

            actor.RemoveData<DualRoleModule>();
            Assert.That(module.Owner, Is.Null);
            Assert.That(module.CleanUpCount, Is.EqualTo(1));
            Assert.DoesNotThrow(() => other.AddData(module));
        }

        [Test]
        public void DestroyReleasesDataOwnership()
        {
            using var first = new ActorModel();
            using var second = new ActorModel();
            var data = new TrackingData();
            first.AddData(data);
            first.Initialize();
            first.Destroy();

            Assert.DoesNotThrow(() => second.AddData(data));
            Assert.That(data.CleanUpCount, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WorldCleanupDetachesAllMembersBeforeCallbacks(bool dispose)
        {
            using var world = new ActorWorld();
            using var first = world.Add(new ActorModel().WithData(new TrackingData()));
            using var second = world.Add(new ActorModel().WithData(new TrackingData()));
            using var third = world.Add(new ActorModel().WithData(new TrackingData()));
            var query = world.Query<TrackingData>();
            var removedDuringCleanup = true;
            var countDuringCleanup = -1;
            third.Destroyed += () =>
            {
                countDuringCleanup = world.Count;
                removedDuringCleanup = world.Remove(second);
            };

            if (dispose)
                world.Dispose();
            else
                world.Clear();

            Assert.That(second.IsInitialized.Value, Is.False);
            Assert.That(first.IsInitialized.Value, Is.False);
            Assert.That(world.Count, Is.Zero);
            Assert.That(query.CandidateCount, Is.Zero);
            Assert.That(countDuringCleanup, Is.Zero);
            Assert.That(removedDuringCleanup, Is.False);
        }

        [Test]
        public void RegistryCleanupEmptiesPreviouslyReturnedViews()
        {
            using var actor = new ActorModel();
            ActorRegistry.CleanUp();

            try
            {
                ActorRegistry.Add(actor);
                var view = ActorRegistry.GetAll(ActorTag.Default);
                Assert.That(view.Count, Is.EqualTo(1));

                ActorRegistry.CleanUp();

                Assert.That(view.Count, Is.Zero);
                Assert.That(ActorRegistry.Count, Is.Zero);
            }
            finally
            {
                ActorRegistry.CleanUp();
            }
        }

        [Test]
        public void BlueprintCollectionsCannotBeMutatedThroughPublicViews()
        {
            var blueprint = ScriptableObject.CreateInstance<ActorBlueprint>();

            try
            {
                var data = (IList<ActorDataProviderBase>)blueprint.Data;
                var behaviours = (IList<ActorBehaviourProviderBase>)blueprint.Behaviours;
                Assert.That(data.IsReadOnly, Is.True);
                Assert.That(behaviours.IsReadOnly, Is.True);
                Assert.Throws<NotSupportedException>(() => data.Add(null));
                Assert.Throws<NotSupportedException>(() => behaviours.Add(null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(blueprint);
            }
        }
    }
}
