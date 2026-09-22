using System;

using NUnit.Framework;
using Unity.Profiling;

namespace Abc.Unity.Tests
{
    public sealed class ActorNetworkPerformanceTests
    {
        [Test]
        public void WarmedNetworkLookupAndSimulationStepAllocateNothing()
        {
            using var world = new ActorWorld();
            using var map = new ActorNetworkMap(1);
            var actor = new ActorModel();
            world.Add(actor);
            var id = new ActorNetworkId(1);
            map.Bind(id, actor);
            var physicsSteps = 0;
            var simulation = new ActorSimulation(world, 0.02f, _ => physicsSteps++);
            Action measure = () =>
            {
                for (var i = 0; i < 10000; i++)
                {
                    map.TryGetActor(id, out var resolved);
                    map.TryGetId(resolved, out _);
                    simulation.Step();
                }
            };
            measure();
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 1,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            measure();
            recorder.Stop();
            Assert.That(physicsSteps, Is.EqualTo(20000));
            Assert.That(recorder.Count, Is.Zero);
            TestContext.Out.WriteLine("ABC network lookup + fixed simulation step: 0 GC.Alloc samples over 10,000 warmed iterations.");
        }
    }
}
