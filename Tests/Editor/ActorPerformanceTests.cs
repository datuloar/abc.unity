using System;
using System.Diagnostics;

using NUnit.Framework;
using Unity.Profiling;

using Abc.Unity;

namespace Abc.Unity.Tests
{
    public sealed class ActorPerformanceTests
    {
        private const int Iterations = 1000000;
        private const int MeasurementSamples = 5;
        private static TrackingData _dataSink;

        private struct QueryAction : IActorQueryAction<TrackingData>
        {
            public int Count;

            public void Execute(ActorModel actor, TrackingData data) => Count++;
        }

        private struct PairQueryAction : IActorQueryAction<TrackingData, SecondaryData>
        {
            public int Count;

            public void Execute(ActorModel actor, TrackingData data1, SecondaryData data2) => Count++;
        }

        [Test]
        public void ExactDataLookupBenchmark()
        {
            using var actor = new ActorModel();
            var data = new TrackingData();
            actor.AddData(data);
            actor.Initialize();

            for (var i = 0; i < 1024; i++)
                _dataSink = actor.GetData<TrackingData>();

            var stopwatch = new Stopwatch();
            var measurements = new double[MeasurementSamples];
            Action measure = () =>
            {
                for (var i = 0; i < Iterations; i++)
                    _dataSink = actor.GetData<TrackingData>();
            };
            using var recorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Internal,
                "GC.Alloc",
                1,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            Measure(measure, Iterations, stopwatch, measurements);
            recorder.Stop();
            var nanoseconds = GetMedian(measurements);
            TestContext.Out.WriteLine($"ABC exact GetData: {nanoseconds:F2} ns/op, {recorder.Count} GC.Alloc samples");
            Assert.That(_dataSink, Is.SameAs(data));
            Assert.That(recorder.Count, Is.Zero);
        }

        [Test]
        public void CommandDispatchBenchmark()
        {
            using var actor = new ActorModel();
            var listener = new AllocationBehaviour();
            actor.AddBehaviour(listener);
            actor.Initialize();

            for (var i = 0; i < 1024; i++)
                actor.SendCommand<TestCommand>();

            var stopwatch = new Stopwatch();
            var measurements = new double[MeasurementSamples];
            Action measure = () =>
            {
                for (var i = 0; i < Iterations; i++)
                    actor.SendCommand<TestCommand>();
            };
            using var recorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Internal,
                "GC.Alloc",
                1,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            Measure(measure, Iterations, stopwatch, measurements);
            recorder.Stop();
            var nanoseconds = GetMedian(measurements);
            TestContext.Out.WriteLine($"ABC command dispatch: {nanoseconds:F2} ns/op, {recorder.Count} GC.Alloc samples");
            Assert.That(listener.Count, Is.EqualTo(Iterations * MeasurementSamples + 1024));
            Assert.That(recorder.Count, Is.Zero);
        }

        [Test]
        public void WorldTickBenchmark()
        {
            const int actorCount = 1000;
            const int frames = 1000;
            using var world = new ActorWorld(actorCount);

            for (var i = 0; i < actorCount; i++)
            {
                var actor = new ActorModel();
                actor.AddBehaviour(new AllocationBehaviour());
                world.Add(actor);
            }

            for (var i = 0; i < 32; i++)
                world.Tick(0.016f);

            var stopwatch = new Stopwatch();
            var measurements = new double[MeasurementSamples];
            Action measure = () =>
            {
                for (var i = 0; i < frames; i++)
                    world.Tick(0.016f);
            };
            using var recorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Internal,
                "GC.Alloc",
                1,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            var operations = actorCount * frames;
            Measure(measure, operations, stopwatch, measurements);
            recorder.Stop();
            var nanoseconds = GetMedian(measurements);
            TestContext.Out.WriteLine($"ABC ActorWorld tick dispatch: {nanoseconds:F2} ns/actor, {recorder.Count} GC.Alloc samples");
            Assert.That(recorder.Count, Is.Zero);
        }

        [Test]
        public void EmptyModelAllocationBenchmark()
        {
            const int count = 20000;
            var actors = new ActorModel[count];
            var before = GC.GetTotalMemory(true);

            for (var i = 0; i < count; i++)
                actors[i] = new ActorModel();

            var allocated = GC.GetTotalMemory(true) - before;
            GC.KeepAlive(actors);
            TestContext.Out.WriteLine($"ABC empty ActorModel: {allocated / (double)count:F2} B/model");

            for (var i = 0; i < count; i++)
                actors[i].Dispose();

            Assert.That(allocated, Is.LessThanOrEqualTo(count * 128L));
        }

        [Test]
        public void OneDataModelAllocationBenchmark()
        {
            const int count = 20000;
            using var warmup = new ActorModel().WithData(new TrackingData());
            var actors = new ActorModel[count];
            var before = GC.GetTotalMemory(true);

            for (var i = 0; i < count; i++)
                actors[i] = new ActorModel().WithData(new TrackingData());

            var allocated = GC.GetTotalMemory(true) - before;
            GC.KeepAlive(actors);
            TestContext.Out.WriteLine($"ABC one-data ActorModel: {allocated / (double)count:F2} B/model including data");

            for (var i = 0; i < count; i++)
                actors[i].Dispose();

            Assert.That(allocated, Is.LessThanOrEqualTo(count * 512L));
        }

        [Test]
        public void OneBehaviourModelAllocationBenchmark()
        {
            const int count = 20000;
            using var warmup = new ActorModel().WithBehaviour(new AllocationBehaviour());
            var actors = new ActorModel[count];
            var before = GC.GetTotalMemory(true);

            for (var i = 0; i < count; i++)
                actors[i] = new ActorModel().WithBehaviour(new AllocationBehaviour());

            var allocated = GC.GetTotalMemory(true) - before;
            GC.KeepAlive(actors);
            TestContext.Out.WriteLine($"ABC one-behaviour ActorModel: {allocated / (double)count:F2} B/model including behaviour");

            for (var i = 0; i < count; i++)
                actors[i].Dispose();

            Assert.That(allocated, Is.LessThanOrEqualTo(count * 1024L));
        }

        [Test]
        public void WorldQueryBenchmark()
        {
            const int actorCount = 1000;
            const int frames = 1000;
            using var world = new ActorWorld(actorCount);

            for (var i = 0; i < actorCount; i++)
                world.Add(new ActorModel().WithData(new TrackingData()));

            var query = world.Query<TrackingData>();
            var action = new QueryAction();

            for (var i = 0; i < 32; i++)
                query.For(ref action);

            var stopwatch = new Stopwatch();
            var measurements = new double[MeasurementSamples];
            Action measure = () =>
            {
                for (var i = 0; i < frames; i++)
                    query.For(ref action);
            };
            using var recorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Internal,
                "GC.Alloc",
                1,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            var operations = actorCount * frames;
            Measure(measure, operations, stopwatch, measurements);
            recorder.Stop();
            var nanoseconds = GetMedian(measurements);
            TestContext.Out.WriteLine($"ABC indexed query: {nanoseconds:F2} ns/actor, {recorder.Count} GC.Alloc samples");
            Assert.That(action.Count, Is.EqualTo(actorCount * (frames * MeasurementSamples + 32)));
            Assert.That(recorder.Count, Is.Zero);
        }

        [Test]
        public void WorldQueryIntersectionBenchmark()
        {
            const int actorCount = 1000;
            const int frames = 1000;
            using var world = new ActorWorld(actorCount);

            for (var i = 0; i < actorCount; i++)
            {
                world.Add(new ActorModel()
                    .WithData(new TrackingData())
                    .WithData(new SecondaryData()));
            }

            var query = world.Query<TrackingData, SecondaryData>();
            var action = new PairQueryAction();

            for (var i = 0; i < 32; i++)
                query.For(ref action);

            var stopwatch = new Stopwatch();
            var measurements = new double[MeasurementSamples];
            Action measure = () =>
            {
                for (var i = 0; i < frames; i++)
                    query.For(ref action);
            };
            using var recorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Internal,
                "GC.Alloc",
                1,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            var operations = actorCount * frames;
            Measure(measure, operations, stopwatch, measurements);
            recorder.Stop();
            var nanoseconds = GetMedian(measurements);
            TestContext.Out.WriteLine($"ABC two-data query: {nanoseconds:F2} ns/actor, {recorder.Count} GC.Alloc samples");
            Assert.That(action.Count, Is.EqualTo(actorCount * (frames * MeasurementSamples + 32)));
            Assert.That(recorder.Count, Is.Zero);
        }

        private static void Measure(Action sample, int operations, Stopwatch stopwatch, double[] measurements)
        {
            for (var i = 0; i < measurements.Length; i++)
            {
                stopwatch.Restart();
                sample();
                stopwatch.Stop();
                measurements[i] = stopwatch.ElapsedTicks * 1000000000d / Stopwatch.Frequency / operations;
            }
        }

        private static double GetMedian(double[] measurements)
        {
            Array.Sort(measurements);
            return measurements[measurements.Length / 2];
        }
    }
}
