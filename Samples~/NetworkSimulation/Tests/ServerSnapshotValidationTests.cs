using System;
using System.Buffers.Binary;

using NUnit.Framework;
using UnityEngine;

namespace Abc.Unity.Samples.NetworkSimulation.Tests
{
    public sealed class ServerSnapshotValidationTests
    {
        [Test]
        public void QuantizationRejectsInvalidIdentityNonFiniteAndOutOfRangeState()
        {
            Assert.That(QuantizedServerSnapshot.TryCreate(default, out _), Is.False);
            foreach (var value in new[] { float.NaN, float.NegativeInfinity, float.PositiveInfinity, -10001f, 10001f })
            {
                var snapshot = new ServerSnapshot(new ActorNetworkId(1), 1, 1, Vector3.one * value, Vector3.zero);
                Assert.That(QuantizedServerSnapshot.TryCreate(in snapshot, out var invalid), Is.False);
                Assert.That(invalid.IsValid, Is.False);
            }
            foreach (var value in new[] { float.NaN, float.PositiveInfinity, -328f, 328f })
            {
                var snapshot = new ServerSnapshot(new ActorNetworkId(1), 1, 1, Vector3.zero, Vector3.one * value);
                Assert.That(QuantizedServerSnapshot.TryCreate(in snapshot, out _), Is.False);
            }
        }

        [Test]
        public void QuantizationRoundTripRespectsCentimeterResolution()
        {
            var snapshot = new ServerSnapshot(new ActorNetworkId(1), 0, 0,
                new Vector3(1.234f, -5.678f, 9999.999f), new Vector3(-1.234f, 5.678f, 327.67f));
            Assert.That(QuantizedServerSnapshot.TryCreate(in snapshot, out var quantized), Is.True);
            var decoded = quantized.ToSnapshot();
            for (var axis = 0; axis < 3; axis++)
            {
                Assert.That(decoded.Position[axis], Is.EqualTo(snapshot.Position[axis]).Within(0.0055f));
                Assert.That(decoded.Velocity[axis], Is.EqualTo(snapshot.Velocity[axis]).Within(0.0055f));
            }
        }

        [Test]
        public void EveryTruncationTrailingBytesUnknownFormatAndMaskAreRejected()
        {
            var baseline = Create(10, 1);
            var current = Create(11, 2);
            var buffer = new byte[ServerSnapshotCodec.MaxPacketBytes];
            foreach (var useDelta in new[] { false, true })
            {
                var encodingBaseline = useDelta ? baseline : default;
                ServerSnapshotCodec.TryWrite(in current, in encodingBaseline, buffer, out var length);
                for (var truncated = 0; truncated < length; truncated++)
                    AssertInvalid(buffer.AsSpan(0, truncated), in baseline);
                AssertInvalid(buffer.AsSpan(0, length + 1), in baseline);
                buffer[0] = 18;
                AssertInvalid(buffer.AsSpan(0, length), in baseline);
            }
            AssertInvalid(new byte[] { 17, 1, 11, 10, 128 }, in baseline);
        }

        [Test]
        public void GoldenWireRecordsAreStableLittleEndianAndCanonical()
        {
            var baseline = Create(10, 1);
            var current = Create(11, 2);
            var buffer = new byte[ServerSnapshotCodec.MaxPacketBytes];
            ServerSnapshotCodec.TryWrite(in current, in baseline, buffer, out var length);
            Assert.That(buffer.AsSpan(0, length).ToArray(), Is.EqualTo(new byte[] { 17, 1, 11, 10, 1, 1 }));
            AssertInvalid(new byte[] { 17, 129, 0, 11, 10, 1, 1 }, in baseline);
            AssertInvalid(new byte[] { 17, 0, 11, 10, 1, 1 }, in baseline);
            AssertInvalid(new byte[] { 17, 128, 128, 128, 128, 128, 128, 128, 128, 128, 2 }, in baseline);
            ServerSnapshotCodec.TryWrite(in current, default, buffer, out length);
            Assert.That(length, Is.EqualTo(22));
            Assert.That(buffer[0], Is.EqualTo(16));
            Assert.That(BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4)), Is.Zero);
        }

        [Test]
        public void WrongIdentityTickBaselineAndArithmeticOverflowAreRejected()
        {
            var baseline = Create(10, ulong.MaxValue - 1);
            AssertInvalid(new byte[] { 17, 2, 11, 10, 0 }, in baseline);
            AssertInvalid(new byte[] { 17, 1, 10, 10, 0 }, in baseline);
            AssertInvalid(new byte[] { 17, 1, 11, 9, 0 }, in baseline);
            AssertInvalid(new byte[] { 17, 1, 11, 10, 1, 2 }, in baseline);
            AssertInvalid(new byte[] { 17, 1, 11, 10, 2, 255, 255, 255, 255, 15 }, in baseline);
            AssertInvalid(new byte[] { 17, 1, 11, 10, 2, 128, 128, 128, 128, 16 }, in baseline);
            var snapshot = new ServerSnapshot(new ActorNetworkId(1), 10, 0, Vector3.one * 10000f, Vector3.zero);
            QuantizedServerSnapshot.TryCreate(in snapshot, out var edge);
            AssertInvalid(new byte[] { 17, 1, 11, 10, 2, 2 }, in edge);
        }

        [Test]
        public void FullWireValuesOutsideQuantizationBoundsAreRejected()
        {
            var current = Create(10, 1);
            var buffer = new byte[22];
            ServerSnapshotCodec.TryWrite(in current, default, buffer, out _);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(4), 1000001);
            AssertInvalid(buffer, default);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(4), 0);
            BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(16), short.MinValue);
            AssertInvalid(buffer, default);
        }

        [Test]
        public void BoundedRandomPacketsNeverThrowOrProduceUnencodableState()
        {
            var random = new System.Random(601);
            var buffer = new byte[64];
            var output = new byte[ServerSnapshotCodec.MaxPacketBytes];
            var baseline = Create(10, 1);
            for (var iteration = 0; iteration < 20000; iteration++)
            {
                random.NextBytes(buffer);
                buffer[0] = (byte)(16 + iteration % 2);
                var payload = buffer.AsSpan(0, random.Next(0, buffer.Length + 1));
                ServerSnapshotCodec.TryGetBaseline(payload, out _, out _);
                if (!ServerSnapshotCodec.TryRead(payload, in baseline, out var decoded))
                    continue;
                Assert.That(ServerSnapshotCodec.TryWrite(in decoded, default, output, out var length), Is.True);
                Assert.That(ServerSnapshotCodec.TryRead(output.AsSpan(0, length), default, out _), Is.True);
            }
        }

        private static QuantizedServerSnapshot Create(ulong tick, ulong input)
        {
            var snapshot = new ServerSnapshot(new ActorNetworkId(1), tick, input, Vector3.zero, Vector3.zero);
            Assert.That(QuantizedServerSnapshot.TryCreate(in snapshot, out var quantized), Is.True);
            return quantized;
        }

        private static void AssertInvalid(ReadOnlySpan<byte> packet, in QuantizedServerSnapshot baseline)
        {
            Assert.That(ServerSnapshotCodec.TryRead(packet, in baseline, out var snapshot), Is.False);
            Assert.That(snapshot.IsValid, Is.False);
        }
    }
}
