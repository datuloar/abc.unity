using System;

using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;

namespace Abc.Unity.Samples.NetworkSimulation.Tests
{
    public sealed class PhysicsServerSessionTests
    {
        private static readonly ActorNetworkId Id = new ActorNetworkId(1);

        [Test]
        public void WarmedInputPhysicsAndSnapshotLoopAllocateNothing()
        {
            using var session = new PhysicsServerSession();
            session.Spawn(Id, 7, Vector3.up);
            var snapshots = new ServerSnapshot[1];
            Action run = () =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    var input = new ServerInput(Id, session.Simulation.Tick + 1, Vector2.zero);
                    session.TryApplyInput(7, in input);
                    session.Simulation.Step();
                    session.CopySnapshots(snapshots);
                }
            };
            run();
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 1,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            run();
            recorder.Stop();
            Assert.That(recorder.Count, Is.Zero);
            Assert.That(snapshots[0].LastInput, Is.EqualTo(2000));
            TestContext.Out.WriteLine("ABC server input + 3D physics + snapshot: 0 GC.Alloc samples over 1,000 warmed steps.");
        }

        [Test]
        public void PhysicsRunsWithoutRenderingAndCollidesWithServerFloor()
        {
            var mode = Physics.simulationMode;
            using var session = new PhysicsServerSession();
            session.Spawn(Id, 7, new Vector3(0f, 4f, 0f));
            var snapshots = new ServerSnapshot[1];
            for (ulong i = 1; i <= 300; i++)
            {
                Assert.That(session.TryApplyInput(7, new ServerInput(Id, i, new Vector2(0.5f, 0f))), Is.True);
                session.Simulation.Step();
            }
            Assert.That(session.CopySnapshots(snapshots), Is.EqualTo(1));
            Assert.That(snapshots[0].Position.y, Is.InRange(0.45f, 0.6f));
            Assert.That(snapshots[0].Position.x, Is.GreaterThan(5f));
            Assert.That(snapshots[0].Tick, Is.EqualTo(300));
            Assert.That(snapshots[0].LastInput, Is.EqualTo(300));
            Assert.That(Physics.simulationMode, Is.EqualTo(mode));
        }

        [Test]
        public void InputRejectsWrongOwnerDuplicateStaleFloodedAndMalformedCommands()
        {
            using var session = new PhysicsServerSession();
            session.Spawn(Id, 7, Vector3.up);
            Assert.That(session.TryApplyInput(8, new ServerInput(Id, 1, Vector2.zero)), Is.False);
            Assert.That(session.TryApplyInput(7, new ServerInput(new ActorNetworkId(2), 1, Vector2.zero)), Is.False);
            Assert.That(session.TryApplyInput(7, new ServerInput(Id, 0, Vector2.zero)), Is.False);
            Assert.That(session.TryApplyInput(7, new ServerInput(Id, ulong.MaxValue, Vector2.zero)), Is.False);
            Assert.That(session.TryApplyInput(7, new ServerInput(Id, 1, new Vector2(float.NaN, 0f))), Is.False);
            Assert.That(session.TryApplyInput(7, new ServerInput(Id, 1, new Vector2(0f, float.PositiveInfinity))), Is.False);
            Assert.That(session.TryApplyInput(7, new ServerInput(Id, 1, Vector2.one)), Is.False);
            Assert.That(session.TryApplyInput(7, new ServerInput(Id, 2, Vector2.right)), Is.True);
            Assert.That(session.TryApplyInput(7, new ServerInput(Id, 3, Vector2.left)), Is.False);
            session.Simulation.Step();
            Assert.That(session.TryApplyInput(7, new ServerInput(Id, 2, Vector2.zero)), Is.False);
            Assert.That(session.TryApplyInput(7, new ServerInput(Id, 1, Vector2.zero)), Is.False);
            Assert.That(session.TryApplyInput(7, new ServerInput(Id, 3, Vector2.zero)), Is.True);
        }

        [Test]
        public void SnapshotAcknowledgesOnlySimulatedInputAndMissingInputExpires()
        {
            using var session = new PhysicsServerSession();
            session.Spawn(Id, 7, Vector3.up * 4f);
            session.TryApplyInput(7, new ServerInput(Id, 1, Vector2.right));
            var snapshots = new ServerSnapshot[1];
            session.CopySnapshots(snapshots);
            Assert.That(snapshots[0].LastInput, Is.Zero);
            session.Simulation.Step();
            session.CopySnapshots(snapshots);
            Assert.That(snapshots[0].LastInput, Is.EqualTo(1));
            Assert.That(snapshots[0].Velocity.x, Is.GreaterThan(0f));
            for (var i = 0; i < 10; i++)
                session.Simulation.Step();
            session.CopySnapshots(snapshots);
            Assert.That(snapshots[0].Velocity.x, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void SessionsWithIdenticalIdsDoNotSharePhysicsOrState()
        {
            using var first = new PhysicsServerSession();
            using var second = new PhysicsServerSession();
            first.Spawn(Id, 1, Vector3.up * 4f);
            second.Spawn(Id, 2, Vector3.up * 4f);
            for (var i = 0; i < 100; i++)
                first.Simulation.Step();
            var snapshots = new ServerSnapshot[1];
            first.CopySnapshots(snapshots);
            Assert.That(snapshots[0].Position.y, Is.LessThan(1f));
            second.CopySnapshots(snapshots);
            Assert.That(snapshots[0].Position.y, Is.EqualTo(4f));
            Assert.That(snapshots[0].Tick, Is.Zero);
        }

        [Test]
        public void SpawnSnapshotAndDespawnContractsRejectInvalidOperations()
        {
            using var session = new PhysicsServerSession();
            Assert.Throws<ArgumentException>(() => session.Spawn(default, 1, Vector3.zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.Spawn(Id, 1, Vector3.one * float.NaN));
            session.Spawn(Id, 1, Vector3.up);
            Assert.Throws<ArgumentException>(() => session.Spawn(Id, 2, Vector3.up));
            Assert.Throws<ArgumentNullException>(() => session.CopySnapshots(null));
            Assert.Throws<ArgumentException>(() => session.CopySnapshots(Array.Empty<ServerSnapshot>()));
            Assert.That(session.Count, Is.EqualTo(1));
            Assert.That(session.Despawn(Id), Is.True);
            Assert.That(session.Despawn(Id), Is.False);
            Assert.That(session.TryApplyInput(1, new ServerInput(Id, 1, Vector2.zero)), Is.False);
            Assert.That(session.Count, Is.Zero);
            session.Dispose();
            Assert.Throws<ObjectDisposedException>(() => session.Spawn(Id, 1, Vector3.zero));
            Assert.Throws<ObjectDisposedException>(session.Simulation.Step);
        }
    }
}
