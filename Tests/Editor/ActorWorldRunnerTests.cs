using NUnit.Framework;
using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Tests
{
    public sealed class ActorWorldRunnerTests
    {
        [Test]
        public void RunnerCreatesDisposesAndRecreatesWorld()
        {
            var gameObject = new GameObject("Runner");

            try
            {
                var runner = gameObject.AddComponent<ActorWorldRunner>();
                var first = runner.GetOrCreateWorld();

                Assert.That(runner.HasWorld, Is.True);
                Assert.That(runner.World, Is.SameAs(first));

                runner.DisposeWorld();

                Assert.That(runner.HasWorld, Is.False);
                Assert.That(first.IsDisposed, Is.True);

                var second = runner.GetOrCreateWorld();
                Assert.That(second, Is.Not.SameAs(first));
                Assert.That(second.Name, Is.EqualTo("Gameplay"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
