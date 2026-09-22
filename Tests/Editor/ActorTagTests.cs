using System;

using NUnit.Framework;
using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Tests
{
    public sealed class ActorTagTests
    {
        [Serializable]
        private sealed class TagContainer
        {
            public ActorTag Tag;
        }

        private struct CountAction : IActorQueryAction<TrackingData>
        {
            public int Count;

            public void Execute(ActorModel actor, TrackingData data) => Count++;
        }

        [SetUp]
        public void SetUp() => ActorRegistry.CleanUp();

        [TearDown]
        public void TearDown() => ActorRegistry.CleanUp();

        [Test]
        public void CustomTagsUseStableValueEquality()
        {
            var first = new ActorTag(1001);
            var second = new ActorTag(1001);
            var different = new ActorTag(1002);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
            Assert.That(first == second, Is.True);
            Assert.That(first != different, Is.True);
        }

        [Test]
        public void CustomTagWorksWithRegistryAndRuntimeChanges()
        {
            var custom = new ActorTag(1001);
            using var actor = new ActorModel("Boss");

            try
            {
                ActorRegistry.Add(actor);
                actor.SetTag(custom);

                Assert.That(ActorRegistry.Get(custom), Is.SameAs(actor));
                Assert.That(ActorRegistry.Has(ActorTag.Default), Is.False);
            }
            finally
            {
                ActorRegistry.Remove(actor);
                ActorRegistry.CleanUp();
            }
        }

        [Test]
        public void BuiltInTagsKeepReadableNames()
        {
            Assert.That(ActorTag.Default.ToString(), Is.EqualTo("Default"));
            Assert.That(ActorTag.Player.ToString(), Is.EqualTo("Player"));
            Assert.That(ActorTag.Enemy.ToString(), Is.EqualTo("Enemy"));
            Assert.That(new ActorTag(1001).ToString(), Is.EqualTo("1001"));
        }

        [Test]
        public void CustomTagWorksWithWorldQueryFilter()
        {
            var custom = new ActorTag(1001);
            using var world = new ActorWorld();
            world.Add(new ActorModel("Boss", custom).WithData(new TrackingData()));
            world.Add(new ActorModel("Enemy", ActorTag.Enemy).WithData(new TrackingData()));
            var action = new CountAction();

            world.Query<TrackingData>().WithTag(custom).For(ref action);

            Assert.That(action.Count, Is.EqualTo(1));
        }

        [Test]
        public void CustomTagSurvivesUnitySerialization()
        {
            var source = new TagContainer { Tag = new ActorTag(1001) };
            var restored = JsonUtility.FromJson<TagContainer>(JsonUtility.ToJson(source));

            Assert.That(restored.Tag, Is.EqualTo(source.Tag));
        }
    }
}
