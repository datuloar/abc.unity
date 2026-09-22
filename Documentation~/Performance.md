# Performance methodology

ABC treats performance as a testable contract rather than a universal benchmark claim.

## Reference environment

The current reference runs use:

- Unity 2022.3.62f2 and Unity 6.0.71f1;
- Editor process;
- Mono scripting runtime;
- Windows 11 x64;
- AMD Ryzen 7 9700X, 8 cores and 16 logical processors;
- warmed generic types, query indexes, and command metadata.

The table records the release-validation run from 2026-09-22.

## Final validation results

| Operation | Unity 2022.3 | Unity 6.0 | Allocation signal |
| --- | ---: | ---: | ---: |
| Indexed one-data query | 1.40 ns/model | 1.33 ns/model | 0 `GC.Alloc` samples |
| Indexed two-data intersection | 7.86 ns/model | 7.36 ns/model | 0 `GC.Alloc` samples |
| Exact `GetData<T>()` | 13.22 ns/op | 11.83 ns/op | 0 `GC.Alloc` samples |
| Command dispatch | 10.80 ns/op | 9.67 ns/op | 0 `GC.Alloc` samples |
| ActorWorld behaviour dispatch | 10.04 ns/model | 8.92 ns/model | 0 `GC.Alloc` samples |
| Empty ActorModel retained memory | 94.82 B/model | 91.75 B/model | not applicable |
| One-data model including data | 393.42 B/model | 388.30 B/model | not applicable |
| One tick-and-command behaviour model | 791.14 B/model | 787.46 B/model | not applicable |

Timing rows are medians from five measured samples after warmup. Results vary with CPU frequency, editor instrumentation, Unity version, scripting backend, managed runtime, and action body. The numbers are useful for catching regressions in the same environment. They are not presented as an apples-to-apples comparison with another framework's component storage or workload.

## Allocation measurement

Hot-path tests use Unity `ProfilerRecorder` with:

```csharp
ProfilerRecorder.StartNew(
    ProfilerCategory.Internal,
    "GC.Alloc",
    1,
    ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
```

`GC.GetAllocatedBytesForCurrentThread` is not used for the Unity Mono allocation assertion because it can report misleading results in this environment. Retained ActorModel footprint uses `GC.GetTotalMemory(true)` across a large live array and is tested with a conservative threshold. These are approximate heap deltas, not object-layout sizes: shared caches, weak ownership-table capacity, test ordering, and collection timing affect the result. Module ownership adds registration-time storage and checks; it does not participate in warmed lookup or dispatch.

## Test shapes

`ExactDataLookupBenchmark` measures five samples of one million exact generic lookups after warmup.

`CommandDispatchBenchmark` measures five samples of one million struct commands sent to one warmed listener through the compact command registry.

`WorldTickBenchmark` measures five samples dispatching 1,000 models across 1,000 frames after warmup.

`WorldQueryBenchmark` measures five samples iterating a cached one-data index for 1,000 models across 1,000 passes using a struct action.

`WorldQueryIntersectionBenchmark` gives every model two data modules and measures five samples of the same one-million-action workload through a two-index intersection.

Retained-memory tests keep 20,000 models alive while measuring the managed heap. The one-behaviour shape deliberately implements both `IActorTick` and a command listener, so it includes both lazy registries.

## Networking allocation coverage

`ActorNetworkPerformanceTests` warms an identity map and fixed simulation before measuring 10,000 combined forward/reverse lookups and steps. Both validated Editor/Mono versions recorded zero `GC.Alloc` samples.

The Network Simulation sample's PlayMode allocation test warms its server session and then measures 1,000 iterations of validated input, fixed gameplay, isolated 3D physics and caller-array snapshot extraction. It recorded zero `GC.Alloc` samples on Unity 2022.3.62f2 and 6000.0.71f1. This one-body reference workload excludes spawn/despawn, binding, serializers, sockets, presentation and game-specific collision callbacks. It is an allocation regression, not a server-capacity or throughput benchmark.

`ServerSnapshotPerformanceTests` separately checks both full and delta encoding. Each case warms 10,000 quantize/encode/decode iterations, then measures another 10,000 with `ProfilerRecorder`. Both modes recorded zero `GC.Alloc` samples on the same two Unity/Mono versions. Five additional batches report median codec-only elapsed time for local diagnostics; that timing excludes physics, sockets, peer history and the game workload and is not a player-capacity claim.

Codec regression vectors assert 22-byte full, 7-byte moving and 5-byte unchanged records for the documented small-ID/counter case. The reader is exercised with every field mask, range/counter boundaries, missing/wrong baselines, truncation, overflow, invalid formats, 10,000 seeded state round trips and 20,000 bounded random payloads. See [Server scale and bandwidth](ServerScaling.md) for the exact schema, bandwidth exclusions and the required end-to-end load-test methodology.

The Windows Mono headless sample also measures its one-body 300-step workload at 60 Hz with 100 snapshots at 20 Hz. Unity 2022.3 and Unity 6 both encoded 866 record bytes, versus 2,316 bytes with forced full encoding of the same quantized states (about 62.6% smaller). This immediate local loopback has no ACK delay, sockets, packet loss, transport overhead or interest management; it is a reproducible sample result, not an online traffic forecast. Reproduce it with the sample README's `-abcServerSmoke` command.

## Running the suite

Open Unity Test Runner and run `abc.unity.tests` in EditMode, or use Unity command line:

```text
Unity.exe -batchmode -nographics -projectPath <project> -runTests -testPlatform EditMode -testResults <results.xml> -logFile <log.txt>
```

The package must be listed under `testables` in the host project's package manifest when installed as a package dependency.

With Network Simulation imported, run its physics and codec tests separately in PlayMode:

```text
Unity.exe -batchmode -nographics -projectPath <project> -runTests -testPlatform PlayMode -testFilter Abc.Unity.Samples.NetworkSimulation.Tests -testResults <network-results.xml> -logFile <network-log.txt>
```

## Player validation

Editor timing does not replace player profiling. For release decisions:

1. build the target platform and scripting backend;
2. profile representative actor counts and action bodies;
3. verify frame-time distribution, retained heap, and allocation markers;
4. compare the complete gameplay workload rather than isolated API calls;
5. keep a device-specific baseline in CI.

The release scope is Unity 2022.3 LTS and Unity 6 on Windows with Windows x64 Mono players. IL2CPP performance and stripping remain a gate for any future expansion of that scope, not a claim made by this release.
