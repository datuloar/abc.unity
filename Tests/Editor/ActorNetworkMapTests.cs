using System;

using NUnit.Framework;
using UnityEngine;

namespace Abc.Unity.Tests
{
    public sealed class ActorNetworkMapTests
    {
        [Test]
        public void NetworkIdKeepsItsFullWireValueAndRejectsZero()
        {
            var id = new ActorNetworkId(ulong.MaxValue);
            Assert.That(id.Value, Is.EqualTo(ulong.MaxValue));
            Assert.That(id == new ActorNetworkId(ulong.MaxValue), Is.True);
            Assert.That(id != default, Is.True);
            Assert.That(default(ActorNetworkId).IsValid, Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => new ActorNetworkId(0));
        }

        [Test]
        public void BindingIsBidirectionalAndIndependentOfWorldStorage()
        {
            using var world = new ActorWorld();
            using var identities = new ActorNetworkMap(2);
            var first = new ActorModel();
            var second = new ActorModel();
            world.Add(first);
            world.Add(second);
            var id = new ActorNetworkId(42);
            identities.Bind(id, second);
            world.Despawn(first);

            Assert.That(identities.TryGetActor(id, out var resolved), Is.True);
            Assert.That(resolved, Is.SameAs(second));
            Assert.That(identities.TryGetId(second, out var resolvedId), Is.True);
            Assert.That(resolvedId, Is.EqualTo(id));
            second.Kill();
            Assert.That(identities.TryGetActor(id, out _), Is.True);
            world.Remove(second);
            Assert.That(identities.TryGetActor(id, out _), Is.True);
            second.Dispose();
            Assert.That(identities.Count, Is.Zero);
        }

        [Test]
        public void BindingRejectsUninitializedDestroyedAndDuplicateActorsWithoutMutation()
        {
            using var identities = new ActorNetworkMap();
            using var first = new ActorModel();
            using var second = new ActorModel();
            var id = new ActorNetworkId(1);
            Assert.Throws<InvalidOperationException>(() => identities.Bind(id, first));
            first.Initialize();
            second.Initialize();
            identities.Bind(id, first);
            Assert.Throws<InvalidOperationException>(() => identities.Bind(id, second));
            Assert.Throws<InvalidOperationException>(() => identities.Bind(new ActorNetworkId(2), first));
            Assert.Throws<ArgumentException>(() => identities.Bind(default, second));
            Assert.Throws<ArgumentNullException>(() => identities.Bind(new ActorNetworkId(3), null));
            second.Destroy();
            Assert.Throws<InvalidOperationException>(() => identities.Bind(new ActorNetworkId(3), second));
            Assert.That(identities.Count, Is.EqualTo(1));
        }

        [Test]
        public void DestructionAndWorldClearRemoveBindings()
        {
            using var identities = new ActorNetworkMap();
            using var world = new ActorWorld();
            for (ulong i = 1; i <= 3; i++)
            {
                var model = new ActorModel();
                world.Add(model);
                identities.Bind(new ActorNetworkId(i), model);
            }
            world.Clear();
            Assert.That(identities.Count, Is.Zero);
            Assert.That(identities.TryGetActor(new ActorNetworkId(2), out _), Is.False);
        }

        [Test]
        public void OldDestructionCallbackCannotRemoveAReplacementBinding()
        {
            using var identities = new ActorNetworkMap();
            using var oldActor = new ActorModel();
            using var replacement = new ActorModel();
            var id = new ActorNetworkId(1);
            oldActor.Initialize();
            replacement.Initialize();
            oldActor.Destroyed += () =>
            {
                identities.Unbind(id);
                identities.Bind(id, replacement);
            };
            identities.Bind(id, oldActor);
            oldActor.Destroy();
            Assert.That(identities.TryGetActor(id, out var actor), Is.True);
            Assert.That(actor, Is.SameAs(replacement));
        }

        [Test]
        public void MapsAreSessionLocalAndDisposalDoesNotDestroyActors()
        {
            using var firstMap = new ActorNetworkMap();
            using var secondMap = new ActorNetworkMap();
            using var first = new ActorModel();
            using var second = new ActorModel();
            first.Initialize();
            second.Initialize();
            var id = new ActorNetworkId(1);
            firstMap.Bind(id, first);
            secondMap.Bind(id, second);
            firstMap.Dispose();
            firstMap.Dispose();
            Assert.That(first.IsInitialized.Value, Is.True);
            Assert.That(firstMap.Count, Is.Zero);
            Assert.That(firstMap.TryGetActor(id, out _), Is.False);
            Assert.Throws<ObjectDisposedException>(() => firstMap.Bind(id, first));
            Assert.That(secondMap.TryGetActor(id, out var resolved), Is.True);
            Assert.That(resolved, Is.SameAs(second));
        }

        [Test]
        public void ClearAndUnbindReleaseSubscriptionsWithoutAffectingNewBindings()
        {
            using var map = new ActorNetworkMap();
            using var first = new ActorModel();
            using var second = new ActorModel();
            first.Initialize();
            second.Initialize();
            var id = new ActorNetworkId(1);
            map.Bind(id, first);
            Assert.That(map.Unbind(id), Is.True);
            Assert.That(map.Unbind(id), Is.False);
            Assert.That(map.TryGetId(first, out _), Is.False);
            map.Bind(id, first);
            map.Clear();
            map.Bind(id, second);
            first.Destroy();
            Assert.That(map.Count, Is.EqualTo(1));
        }

        [Test]
        public void SceneActorCanBeBoundAndDestroyed()
        {
            using var map = new ActorNetworkMap();
            var gameObject = new GameObject("Network Actor");
            try
            {
                var actor = gameObject.AddComponent<Actor>();
                actor.Initialize();
                map.Bind(new ActorNetworkId(1), actor);
                actor.Destroy();
                Assert.That(map.Count, Is.Zero);
            }
            finally
            {
                if (gameObject != null)
                    UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
