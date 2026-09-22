using System;
using System.Diagnostics;

using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;

namespace Abc.Unity.Samples.NetworkSimulation.Tests
{
    public sealed class ServerSnapshotPerformanceTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void WarmedQuantizationEncodeDecodeLoopAllocatesNothing(bool delta)
        {
            var source = new ServerSnapshot(new ActorNetworkId(7), 101, 101, Vector3.right * 0.05f, Vector3.right);
            var previous = new ServerSnapshot(new ActorNetworkId(7), 100, 100, Vector3.zero, Vector3.right);
            QuantizedServerSnapshot.TryCreate(in previous, out var baseline);
            if (!delta)
                baseline = default;
            var buffer = new byte[ServerSnapshotCodec.MaxPacketBytes];
            var checksum = 0UL;
            Action run = () =>
            {
                for (var iteration = 0; iteration < 10000; iteration++)
                {
                    if (!QuantizedServerSnapshot.TryCreate(in source, out var quantized) ||
                        !ServerSnapshotCodec.TryWrite(in quantized, in baseline, buffer, out var length) ||
                        !ServerSnapshotCodec.TryRead(buffer.AsSpan(0, length), in baseline, out var decoded))
                        throw new InvalidOperationException("Snapshot codec failed.");
                    checksum += decoded.Tick;
                }
            };
            run();
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 1,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            run();
            recorder.Stop();
            Assert.That(recorder.Count, Is.Zero);
            Assert.That(checksum, Is.EqualTo(2020000UL));
            var timings = new double[5];
            var stopwatch = new Stopwatch();
            for (var sample = 0; sample < timings.Length; sample++)
            {
                stopwatch.Restart();
                run();
                stopwatch.Stop();
                timings[sample] = stopwatch.Elapsed.TotalMilliseconds;
            }
            Array.Sort(timings);
            var encoding = delta ? "delta" : "full";
            TestContext.Out.WriteLine($"ABC codec quantize + {encoding} encode + decode: 0 GC.Alloc, median {timings[2]:F3} ms/10,000 records (5 warmed samples). No sockets or physics.");
        }
    }
}
