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

## Running the suite

Open Unity Test Runner and run `abc.unity.tests` in EditMode, or use Unity command line:

```text
Unity.exe -batchmode -nographics -projectPath <project> -runTests -testPlatform EditMode -testResults <results.xml> -logFile <log.txt>
```

The package must be listed under `testables` in the host project's package manifest when installed as a package dependency.

## Player validation

Editor timing does not replace player profiling. For release decisions:

1. build the target platform and scripting backend;
2. profile representative actor counts and action bodies;
3. verify frame-time distribution, retained heap, and allocation markers;
4. compare the complete gameplay workload rather than isolated API calls;
5. keep a device-specific baseline in CI.

The current repository has been compiled and tested on Unity 2022.3 LTS and Unity 6 and has produced a Windows Mono player. IL2CPP was not available on the validation machine, so IL2CPP performance and stripping remain a required release-gate check.
