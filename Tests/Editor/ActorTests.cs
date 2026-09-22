using System;
using System.Text.RegularExpressions;

using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;

using Abc.Unity;

namespace Abc.Unity.Tests
{
    public sealed class ActorTests
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
            DestroyAll<Actor>();
            DestroyAll<ActorRegistryHost>();
            DestroyAll<ActorUpdateScheduler>();
        }

        [Test]
        public void DataResolvesByInterfaceAndRunsLifecycleOnce()
        {
            var actor = CreateActor();
            var data = new TrackingData();

            actor.AddData(data);
            actor.Initialize();

            Assert.That(actor.GetData<ITestDataView>(), Is.SameAs(data));
            Assert.That(data.PreInitializeCount, Is.EqualTo(1));
            Assert.That(data.InitializeCount, Is.EqualTo(1));

            actor.RemoveData<ITestDataView>();

            Assert.That(data.CleanUpCount, Is.EqualTo(1));
            Assert.That(actor.HasData<TrackingData>(), Is.False);
        }

        [Test]
        public void AmbiguousInterfaceLookupIsRejected()
        {
            var actor = CreateActor();
            actor.AddData(new TrackingData());
            actor.AddData(new SecondaryData());

            Assert.That(actor.HasData<ITestDataView>(), Is.False);
            Assert.That(actor.TryGetData<ITestDataView>(out _), Is.False);
            Assert.Throws<InvalidOperationException>(() => actor.GetData<ITestDataView>());
        }

        [Test]
        public void MultipleBehavioursRemainRegisteredAndReceiveCommands()
        {
            var actor = CreateActor();
            var first = new FirstListener();
            var second = new SecondListener();
            actor.AddBehaviour(first);
            actor.AddBehaviour(second);
            actor.Initialize();

            actor.SendCommand<TestCommand>();

            Assert.That(first.CommandCount, Is.EqualTo(1));
            Assert.That(second.CommandCount, Is.EqualTo(1));
            Assert.That(actor.GetBehaviour<FirstListener>(), Is.SameAs(first));

            actor.RemoveBehaviour<FirstListener>();
            actor.SendCommand<TestCommand>();

            Assert.That(first.CommandCount, Is.EqualTo(1));
            Assert.That(first.CleanUpCount, Is.EqualTo(1));
            Assert.That(second.CommandCount, Is.EqualTo(2));
        }

        [Test]
        public void FailingCommandListenerDoesNotSkipRemainingListeners()
        {
            var actor = CreateActor();
            var second = new SecondListener();
            actor.AddBehaviour(new ThrowingListener());
            actor.AddBehaviour(second);
            actor.Initialize();
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: Command failure"));

            actor.SendCommand<TestCommand>();

            Assert.That(second.CommandCount, Is.EqualTo(1));
        }

        [Test]
        public void RemovingBehaviourDuringTickDoesNotSkipNextBehaviour()
        {
            var actor = CreateActor();
            var removing = new SelfRemovingBehaviour();
            var counting = new CountingBehaviour();
            actor.AddBehaviour(removing);
            actor.AddBehaviour(counting);
            actor.Initialize();
            actor.gameObject.SetActive(true);

            actor.Tick(0.016f);

            Assert.That(removing.TickCount, Is.EqualTo(1));
            Assert.That(removing.CleanUpCount, Is.EqualTo(1));
            Assert.That(counting.TickCount, Is.EqualTo(1));
            Assert.That(actor.HasBehaviour<SelfRemovingBehaviour>(), Is.False);
        }

        [Test]
        public void KillAndReviveControlTicks()
        {
            var actor = CreateActor();
            var counting = new CountingBehaviour();
            actor.AddBehaviour(counting);
            actor.Initialize();
            actor.gameObject.SetActive(true);

            actor.Tick(0.016f);
            actor.Kill();
            actor.Tick(0.016f);
            actor.Revive();
            actor.Tick(0.016f);

            Assert.That(counting.TickCount, Is.EqualTo(2));
        }

        [Test]
        public void DestroyDuringModuleInitializationCompletesShutdown()
        {
            var actor = CreateActor();
            var behaviour = new DestroyOnInitializeBehaviour();
            var destroyedCount = 0;
            actor.AddBehaviour(behaviour);
            actor.Destroyed += () => destroyedCount++;

            Assert.DoesNotThrow(actor.Initialize);
            Assert.That(destroyedCount, Is.EqualTo(1));
            Assert.That(behaviour.CleanUpCount, Is.EqualTo(1));
        }

        [Test]
        public void DestroyFromContainerAddedCallbackDoesNotReregisterActor()
        {
            var actor = CreateActor();
            var destroyedCount = 0;
            Action<IActor> handler = addedActor =>
            {
                if (ReferenceEquals(addedActor, actor))
                    actor.Destroy();
            };

            actor.Destroyed += () => destroyedCount++;
            ActorRegistry.Added += handler;

            try
            {
                Assert.DoesNotThrow(actor.Initialize);
                Assert.That(destroyedCount, Is.EqualTo(1));
                Assert.That(ActorRegistry.Count, Is.Zero);
            }
            finally
            {
                ActorRegistry.Added -= handler;
            }
        }

        [Test]
        public void ParentActorDoesNotCaptureNestedActorModules()
        {
            var parentObject = new GameObject("Parent");
            parentObject.SetActive(false);
            var parent = parentObject.AddComponent<Actor>();
            var childObject = new GameObject("Child");
            childObject.transform.SetParent(parentObject.transform);
            var child = childObject.AddComponent<Actor>();
            var childData = childObject.AddComponent<TestComponentData>();

            parent.Initialize();
            child.Initialize();

            Assert.That(parent.HasData<TestComponentData>(), Is.False);
            Assert.That(child.GetData<TestComponentData>(), Is.SameAs(childData));
        }

        [Test]
        public void ContainerReindexesActorWhenTagChanges()
        {
            var actor = CreateActor();
            actor.SetTag(ActorTag.Player);
            actor.Initialize();

            Assert.That(ActorRegistry.Get(ActorTag.Player), Is.SameAs(actor));

            actor.SetTag(ActorTag.Enemy);

            Assert.That(ActorRegistry.Has(ActorTag.Player), Is.False);
            Assert.That(ActorRegistry.Get(ActorTag.Enemy), Is.SameAs(actor));
        }

        [Test]
        public void CachedTagViewRemainsLiveAfterTagBecomesEmpty()
        {
            var first = CreateActor();
            first.SetTag(ActorTag.Enemy);
            first.Initialize();
            var enemies = ActorRegistry.GetAll(ActorTag.Enemy);

            first.Destroy();

            var second = CreateActor();
            second.SetTag(ActorTag.Enemy);
            second.Initialize();

            Assert.That(enemies.Count, Is.EqualTo(1));
            Assert.That(enemies[0], Is.SameAs(second));
        }

        [Test]
        public void ContainerMaintainsTagIndexesAfterSwapBackRemoval()
        {
            var first = CreateActor();
            var second = CreateActor();
            var third = CreateActor();
            first.SetTag(ActorTag.Enemy);
            second.SetTag(ActorTag.Enemy);
            third.SetTag(ActorTag.Enemy);
            first.Initialize();
            second.Initialize();
            third.Initialize();

            second.Destroy();
            third.SetTag(ActorTag.Player);

            Assert.That(ActorRegistry.GetAll(ActorTag.Enemy), Has.Count.EqualTo(1));
            Assert.That(ActorRegistry.Get(ActorTag.Enemy), Is.SameAs(first));
            Assert.That(ActorRegistry.Get(ActorTag.Player), Is.SameAs(third));
        }

        [Test]
        public void DestroyEventAndCleanupRunExactlyOnce()
        {
            var actor = CreateActor();
            var data = new TrackingData();
            var destroyedCount = 0;
            actor.AddData(data);
            actor.Initialize();
            actor.Destroyed += () => destroyedCount++;

            actor.Destroy();

            Assert.That(destroyedCount, Is.EqualTo(1));
            Assert.That(data.CleanUpCount, Is.EqualTo(1));
        }

        [Test]
        public void BehaviourProviderReturnsIndependentInstances()
        {
            var provider = ScriptableObject.CreateInstance<CloneBehaviourProvider>();

            try
            {
                var first = (CloneBehaviour)provider.GetBehaviour();
                var second = (CloneBehaviour)provider.GetBehaviour();

                Assert.That(first, Is.Not.SameAs(second));
                Assert.That(first.Value, Is.EqualTo(7));
                Assert.That(second.Value, Is.EqualTo(7));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(provider);
            }
        }

        [Test]
        public void DataProviderClonesSerializableDataWithoutManualCloneMethod()
        {
            var provider = ScriptableObject.CreateInstance<SerializedDataProvider>();

            try
            {
                var first = (SerializedData)provider.GetData();
                var second = (SerializedData)provider.GetData();

                Assert.That(first, Is.Not.SameAs(second));
                Assert.That(first.Value, Is.EqualTo(17));
                Assert.That(second.Value, Is.EqualTo(17));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(provider);
            }
        }

        [Test]
        public void TickAndCommandDispatchDoNotAllocateAfterWarmup()
        {
            var actor = CreateActor();
            actor.AddBehaviour(new AllocationBehaviour());
            actor.Initialize();
            actor.gameObject.SetActive(true);

            for (var i = 0; i < 32; i++)
            {
                actor.Tick(0.016f);
                actor.SendCommand<TestCommand>();
            }

            using var recorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Internal,
                "GC.Alloc",
                1,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);

            for (var i = 0; i < 1024; i++)
            {
                actor.Tick(0.016f);
                actor.SendCommand<TestCommand>();
            }

            recorder.Stop();
            Assert.That(recorder.Count, Is.Zero);
        }

        private static Actor CreateActor()
        {
            var gameObject = new GameObject("Actor Test");
            gameObject.SetActive(false);
            return gameObject.AddComponent<Actor>();
        }

        private static void DestroyAll<T>() where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            var objects = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var objects = UnityEngine.Object.FindObjectsOfType<T>(true);
#endif

            for (var i = 0; i < objects.Length; i++)
            {
                var instance = objects[i];
                if (instance == null)
                    continue;

                if (instance is Component component)
                    UnityEngine.Object.DestroyImmediate(component.gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
