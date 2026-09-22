using NUnit.Framework;
using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Tests
{
    public sealed class ActorRegistryTests
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
#if UNITY_2023_1_OR_NEWER
            var actors = Object.FindObjectsByType<Actor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var actors = Object.FindObjectsOfType<Actor>(true);
#endif

            for (var i = 0; i < actors.Length; i++)
            {
                if (actors[i] != null)
                    Object.DestroyImmediate(actors[i].gameObject);
            }

            ActorRegistry.CleanUp();
            ActorUpdateScheduler.CleanUp();
        }

        [Test]
        public void ExactLookupSurvivesLargeMapTransitions()
        {
            var actor = CreateActor();
            actor.AddData(new IndexedData<MarkerOne>(1));
            actor.AddData(new IndexedData<MarkerTwo>(2));
            actor.AddData(new IndexedData<MarkerThree>(3));
            actor.AddData(new IndexedData<MarkerFour>(4));
            actor.AddData(new IndexedData<MarkerFive>(5));
            actor.AddData(new IndexedData<MarkerSix>(6));
            actor.AddData(new IndexedData<MarkerSeven>(7));
            actor.AddData(new IndexedData<MarkerEight>(8));
            actor.AddData(new IndexedData<MarkerNine>(9));
            actor.AddData(new IndexedData<MarkerTen>(10));

            Assert.That(actor.GetData<IndexedData<MarkerOne>>().Value, Is.EqualTo(1));
            Assert.That(actor.GetData<IndexedData<MarkerFive>>().Value, Is.EqualTo(5));
            Assert.That(actor.GetData<IndexedData<MarkerTen>>().Value, Is.EqualTo(10));

            actor.RemoveData<IndexedData<MarkerNine>>();
            actor.RemoveData<IndexedData<MarkerTen>>();

            Assert.That(actor.HasData<IndexedData<MarkerNine>>(), Is.False);
            Assert.That(actor.HasData<IndexedData<MarkerTen>>(), Is.False);
            Assert.That(actor.GetData<IndexedData<MarkerOne>>().Value, Is.EqualTo(1));
            Assert.That(actor.GetData<IndexedData<MarkerEight>>().Value, Is.EqualTo(8));
        }

        [Test]
        public void MultiCommandRegistrationSurvivesCompactEntryRemoval()
        {
            var actor = CreateActor();
            var multi = new MultiCommandListener();
            var primary = new SecondListener();
            actor.AddBehaviour(multi);
            actor.AddBehaviour(primary);
            actor.Initialize();

            actor.SendCommand<TestCommand>();
            actor.SendCommand<AlternateTestCommand>();

            Assert.That(multi.PrimaryCount, Is.EqualTo(1));
            Assert.That(multi.AlternateCount, Is.EqualTo(1));
            Assert.That(primary.CommandCount, Is.EqualTo(1));

            actor.RemoveBehaviour<MultiCommandListener>();
            actor.SendCommand<TestCommand>();
            actor.SendCommand<AlternateTestCommand>();

            Assert.That(multi.PrimaryCount, Is.EqualTo(1));
            Assert.That(multi.AlternateCount, Is.EqualTo(1));
            Assert.That(primary.CommandCount, Is.EqualTo(2));
        }

        [Test]
        public void RemovingCommandListenerDuringDispatchDoesNotSkipNextListener()
        {
            var actor = CreateActor();
            var removing = new SelfRemovingCommandListener();
            var remaining = new SecondListener();
            actor.AddBehaviour(removing);
            actor.AddBehaviour(remaining);
            actor.Initialize();

            actor.SendCommand<TestCommand>();
            actor.SendCommand<TestCommand>();

            Assert.That(removing.CommandCount, Is.EqualTo(1));
            Assert.That(remaining.CommandCount, Is.EqualTo(2));
            Assert.That(actor.HasBehaviour<SelfRemovingCommandListener>(), Is.False);
        }

        private static Actor CreateActor()
        {
            var gameObject = new GameObject("Actor Registry Test");
            gameObject.SetActive(false);
            return gameObject.AddComponent<Actor>();
        }
    }
}
