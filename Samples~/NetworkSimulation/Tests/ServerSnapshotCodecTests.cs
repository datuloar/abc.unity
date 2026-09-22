using System;

using NUnit.Framework;
using UnityEngine;

namespace Abc.Unity.Samples.NetworkSimulation.Tests
{
    public sealed class ServerSnapshotCodecTests
    {
        [Test]
        public void FullSnapshotRoundTripsBoundaryIdsCountersAndCoordinates()
        {
            var values = new[] { 1UL, 127UL, 128UL, 16384UL, ulong.MaxValue };
            var buffer = new byte[ServerSnapshotCodec.MaxPacketBytes];
            foreach (var value in values)
            {
                var snapshot = Create(value, value, new Vector3(10000f, -10000f, 0f),
                    new Vector3(327.67f, -327.67f, 0f), value);
                Assert.That(ServerSnapshotCodec.TryWrite(in snapshot, default, buffer, out var length), Is.True);
                Assert.That(ServerSnapshotCodec.TryGetBaseline(buffer.AsSpan(0, length), out _, out _), Is.False);
                Assert.That(ServerSnapshotCodec.TryRead(buffer.AsSpan(0, length), default, out var decoded), Is.True);
                AssertSame(in snapshot, in decoded);
            }
        }

        [Test]
        public void MeasuredSmallIdRecordsUse22BytesFull7MovingAnd5Unchanged()
        {
            var baseline = Create(100, 100, Vector3.zero, Vector3.right);
            var moving = Create(101, 101, Vector3.right * 0.05f, Vector3.right);
            var idle = Create(101, 100, Vector3.zero, Vector3.right);
            Span<byte> packet = stackalloc byte[ServerSnapshotCodec.MaxPacketBytes];
            Assert.That(ServerSnapshotCodec.TryWrite(in moving, default, packet, out var fullSize), Is.True);
            Assert.That(ServerSnapshotCodec.TryWrite(in moving, in baseline, packet, out var movingSize), Is.True);
            Assert.That(ServerSnapshotCodec.TryWrite(in idle, in baseline, packet, out var idleSize), Is.True);
            Assert.That(fullSize, Is.EqualTo(22));
            Assert.That(movingSize, Is.EqualTo(7));
            Assert.That(idleSize, Is.EqualTo(5));
            TestContext.Out.WriteLine($"ABC codec record sizes: full={fullSize}, moving={movingSize}, unchanged={idleSize} bytes.");
        }

        [Test]
        public void EveryFieldMaskRoundTripsIndependently()
        {
            var baseline = Create(10, 0, Vector3.zero, Vector3.zero);
            Span<byte> buffer = stackalloc byte[ServerSnapshotCodec.MaxPacketBytes];
            for (var mask = 0; mask < 128; mask++)
            {
                var position = new Vector3();
                var velocity = new Vector3();
                for (var axis = 0; axis < 3; axis++)
                {
                    position[axis] = (mask & (2 << axis)) == 0 ? 0f : -0.01f;
                    velocity[axis] = (mask & (16 << axis)) == 0 ? 0f : 0.01f;
                }
                var snapshot = Create(11, (ulong)(mask & 1), position, velocity);
                Assert.That(ServerSnapshotCodec.TryWrite(in snapshot, in baseline, buffer, out var length), Is.True);
                Assert.That(buffer[4], Is.EqualTo(mask));
                Assert.That(ServerSnapshotCodec.TryRead(buffer.Slice(0, length), in baseline, out var decoded), Is.True);
                AssertSame(in snapshot, in decoded);
            }
        }

        [Test]
        public void LargeChangesFallBackToFullAndNeverExceedMaximumSize()
        {
            var baseline = Create(ulong.MaxValue - 1, 0, Vector3.one * -10000f,
                Vector3.one * -327.67f, ulong.MaxValue);
            var current = Create(ulong.MaxValue, ulong.MaxValue, Vector3.one * 10000f,
                Vector3.one * 327.67f, ulong.MaxValue);
            Span<byte> packet = stackalloc byte[ServerSnapshotCodec.MaxPacketBytes];
            Assert.That(ServerSnapshotCodec.TryWrite(in current, in baseline, packet, out var length), Is.True);
            Assert.That(length, Is.EqualTo(ServerSnapshotCodec.MaxPacketBytes));
            Assert.That(ServerSnapshotCodec.TryGetBaseline(packet, out _, out _), Is.False);
            Assert.That(ServerSnapshotCodec.TryRead(packet, default, out var decoded), Is.True);
            AssertSame(in current, in decoded);
        }

        [Test]
        public void DroppedIntermediateRecordDoesNotAffectDeltaFromAcknowledgedBaseline()
        {
            var acknowledged = Create(100, 100, Vector3.zero, Vector3.right);
            var lost = Create(101, 101, Vector3.right * 0.05f, Vector3.right);
            var current = Create(102, 102, Vector3.right * 0.1f, Vector3.right);
            Span<byte> packet = stackalloc byte[ServerSnapshotCodec.MaxPacketBytes];
            ServerSnapshotCodec.TryWrite(in lost, in acknowledged, packet, out _);
            ServerSnapshotCodec.TryWrite(in current, in acknowledged, packet, out var length);
            var payload = packet.Slice(0, length);
            Assert.That(ServerSnapshotCodec.TryGetBaseline(payload, out var actorId, out var tick), Is.True);
            Assert.That(actorId, Is.EqualTo(acknowledged.ActorId));
            Assert.That(tick, Is.EqualTo(100));
            Assert.That(ServerSnapshotCodec.TryRead(payload, in lost, out _), Is.False);
            Assert.That(ServerSnapshotCodec.TryRead(payload, default, out _), Is.False);
            Assert.That(ServerSnapshotCodec.TryRead(payload, in acknowledged, out var decoded), Is.True);
            AssertSame(in current, in decoded);
            ServerSnapshotCodec.TryWrite(in current, default, packet, out length);
            Assert.That(ServerSnapshotCodec.TryRead(packet.Slice(0, length), default, out decoded), Is.True);
            AssertSame(in current, in decoded);
        }

        [Test]
        public void UnusableBaselineFallsBackToIndependentFullState()
        {
            var current = Create(10, 10, Vector3.zero, Vector3.zero);
            var baselines = new[]
            {
                default(QuantizedServerSnapshot),
                Create(9, 9, Vector3.zero, Vector3.zero, 8),
                Create(10, 10, Vector3.zero, Vector3.zero),
                Create(11, 11, Vector3.zero, Vector3.zero),
                Create(9, 11, Vector3.zero, Vector3.zero)
            };
            var buffer = new byte[ServerSnapshotCodec.MaxPacketBytes];
            foreach (var baseline in baselines)
            {
                Assert.That(ServerSnapshotCodec.TryWrite(in current, in baseline, buffer, out var length), Is.True);
                Assert.That(ServerSnapshotCodec.TryGetBaseline(buffer.AsSpan(0, length), out _, out _), Is.False);
                Assert.That(ServerSnapshotCodec.TryRead(buffer.AsSpan(0, length), default, out var decoded), Is.True);
                AssertSame(in current, in decoded);
            }
        }

        [Test]
        public void ShortDestinationAndInvalidStateDoNotWritePartialRecords()
        {
            var current = Create(10, 10, Vector3.zero, Vector3.zero);
            var buffer = new byte[] { 201, 201, 201 };
            Assert.That(ServerSnapshotCodec.TryWrite(in current, default, buffer, out var written), Is.False);
            Assert.That(written, Is.Zero);
            Assert.That(ServerSnapshotCodec.TryWrite(default, default, buffer, out written), Is.False);
            Assert.That(written, Is.Zero);
            Assert.That(buffer, Is.EqualTo(new byte[] { 201, 201, 201 }));
        }

        [Test]
        public void SeededVariedStatesRoundTripAcrossDeltaAndFullEncodings()
        {
            var random = new System.Random(173);
            Span<byte> buffer = stackalloc byte[ServerSnapshotCodec.MaxPacketBytes];
            var baseline = Create(0, 0, Vector3.zero, Vector3.zero);
            for (ulong tick = 1; tick <= 10000; tick++)
            {
                var position = new Vector3(random.Next(-1000000, 1000001),
                    random.Next(-1000000, 1000001), random.Next(-1000000, 1000001)) / 100f;
                var velocity = new Vector3(random.Next(-32767, 32768),
                    random.Next(-32767, 32768), random.Next(-32767, 32768)) / 100f;
                var current = Create(tick, tick, position, velocity);
                Assert.That(ServerSnapshotCodec.TryWrite(in current, in baseline, buffer, out var length), Is.True);
                Assert.That(ServerSnapshotCodec.TryRead(buffer.Slice(0, length), in baseline, out var decoded), Is.True);
                AssertSame(in current, in decoded);
                baseline = current;
            }
        }

        private static QuantizedServerSnapshot Create(ulong tick, ulong input, Vector3 position,
            Vector3 velocity, ulong id = 7)
        {
            var snapshot = new ServerSnapshot(new ActorNetworkId(id), tick, input, position, velocity);
            Assert.That(QuantizedServerSnapshot.TryCreate(in snapshot, out var quantized), Is.True);
            return quantized;
        }

        private static void AssertSame(in QuantizedServerSnapshot expected, in QuantizedServerSnapshot actual)
        {
            Assert.That(actual.ActorId, Is.EqualTo(expected.ActorId));
            Assert.That(actual.Tick, Is.EqualTo(expected.Tick));
            Assert.That(actual.LastInput, Is.EqualTo(expected.LastInput));
            Assert.That(actual.Position, Is.EqualTo(expected.Position));
            Assert.That(actual.Velocity, Is.EqualTo(expected.Velocity));
        }
    }
}
