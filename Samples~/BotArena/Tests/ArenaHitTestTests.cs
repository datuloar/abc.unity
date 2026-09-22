using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;

namespace Abc.Unity.Samples.BotArena.Tests
{
    public sealed class ArenaHitTestTests
    {
        [Test]
        public void FastProjectileHitsBetweenFrames()
        {
            Assert.That(ArenaHitTest.TryHit(Vector3.left * 10f, Vector3.right * 10f, 1f, out var fraction), Is.True);
            Assert.That(fraction, Is.EqualTo(0.45f).Within(0.0001f));
        }

        [Test]
        public void HitUsesEntryPointInsteadOfClosestApproach()
        {
            Assert.That(ArenaHitTest.TryHit(Vector3.left * 3f, Vector3.right, 1f, out var fraction), Is.True);
            Assert.That(fraction, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void MovingTargetCanCrossStationaryProjectile()
        {
            var projectile = Vector3.zero;
            var targetStart = Vector3.left * 5f;
            var targetEnd = Vector3.right * 5f;

            Assert.That(ArenaHitTest.TryHit(projectile - targetStart, projectile - targetEnd, 1f, out _), Is.True);
        }

        [Test]
        public void InitialOverlapHitsWithoutMovement()
        {
            Assert.That(ArenaHitTest.TryHit(Vector3.zero, Vector3.zero, 1f, out var fraction), Is.True);
            Assert.That(fraction, Is.Zero);
        }

        [Test]
        public void StationarySeparatedProjectileMisses()
        {
            Assert.That(ArenaHitTest.TryHit(Vector3.right * 2f, Vector3.right * 2f, 1f, out _), Is.False);
        }

        [Test]
        public void MovingAwayMisses()
        {
            Assert.That(ArenaHitTest.TryHit(Vector3.right * 2f, Vector3.right * 4f, 1f, out _), Is.False);
        }

        [Test]
        public void PathBeyondLifetimeMisses()
        {
            Assert.That(ArenaHitTest.TryHit(Vector3.left * 5f, Vector3.left * 2f, 1f, out _), Is.False);
        }

        [Test]
        public void OffAxisPathMisses()
        {
            Assert.That(ArenaHitTest.TryHit(new Vector3(-5f, 2f, 0f), new Vector3(5f, 2f, 0f), 1f, out _), Is.False);
        }

        [Test]
        public void RepeatedHitTestsAllocateNoGarbage()
        {
            ArenaHitTest.TryHit(Vector3.left * 10f, Vector3.right * 10f, 1f, out _);
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 1,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);

            for (var i = 0; i < 10000; i++)
                ArenaHitTest.TryHit(Vector3.left * 10f, Vector3.right * 10f, 1f, out _);

            recorder.Stop();
            Assert.That(recorder.Count, Is.Zero);
        }
    }
}
