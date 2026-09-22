# Market and design comparison

This comparison was reviewed against the linked public documentation on 2026-09-22. It compares product shape and published workflows, not unrelated benchmark numbers.

## Position

ABC targets teams that want MonoBehaviour-level iteration speed, explicit object-oriented gameplay code, and a path to large scene-free model sets without translating the game into an ECS architecture.

Its concrete setup surface is intentionally small:

- one runtime namespace: `Abc.Unity`;
- zero required generator DLLs, analyzer labels, API key declarations, or editor restarts;
- zero paid editor dependencies;
- one composition model for GameObjects and scene-free simulation;
- one optional editor scaffold that emits visible source and then gets out of the way.

## Landscape

| Framework | Published model | Strongest fit | Setup and authoring distinction |
| --- | --- | --- | --- |
| ABC | Reference data and behaviours on `Actor` or `ActorModel`; optional indexed `ActorWorld` | Gameplay architecture from prototype to large object-model simulations | Unity 2022.3+, one namespace, built-in inspectors, optional visible scaffold, no required codegen |
| [Atomic](https://github.com/StarKRE22/Atomic) | Object-oriented entities composed from values, variables, events, actions, behaviours, installers, worlds, and filters | Broad object-oriented gameplay toolkit | Requires Unity 6; its published quick start configures four generator/analyzer DLLs and key declarations; Odin is optional and recommended for richer inspection |
| [Massive ECS](https://github.com/nilpunch/massive-ecs) | Bitset-based struct ECS with engine-independent core and snapshot/rollback features | Deterministic prediction, replay, rollback, and value-component simulation | Explicit world, struct component, aspect, and query workflow; Unity integration is separate |
| [StaticEcs](https://github.com/Felid-Force-Studios/StaticEcs) | Static-generic, struct, SoA ECS with hierarchical inverted bitmaps | Very high entity counts, stable storage, parallel iteration, Burst-oriented workloads | Explicit world/system marker types and component registration; dedicated Unity module |
| [Unity Entities](https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/index.html) | Archetype-and-chunk ECS integrated with Jobs, Burst, baking, and subscenes | Maximum data-oriented throughput inside the Unity DOTS stack | Requires ECS component/system rules and a GameObject-to-entity authoring path |

## Atomic

Atomic is the closest product-shape competitor. Its documentation is strong, its object-oriented primitives are broad, and its worlds, filters, factories, pooling, baking, events, and reactive elements cover more scenarios than a narrow actor container.

ABC deliberately competes on a smaller cognitive and setup surface. Atomic's [published quick start](https://github.com/StarKRE22/Atomic#-unity-quick-start) begins by configuring generator and analyzer DLLs, then declares a partial API with typed keys, creates a behaviour, creates an installer, and connects that installer in the scene. Its [requirements](https://github.com/StarKRE22/Atomic#-requirements) state Unity 6+ and describe Odin Inspector as optional but valuable.

ABC starts with a normal data class and behaviour. The C# type is the key, dependencies resolve once, and the same modules run on a GameObject or in a scene-free world. Its built-in inspector, Blueprint editor, Dashboard, and Feature Scaffold do not depend on Odin. The tradeoff is deliberate: ABC currently provides fewer high-level reactive primitives than Atomic.

## Massive ECS and StaticEcs

Massive ECS and StaticEcs are serious performance-oriented ECS choices, not APIs ABC should imitate cosmetically.

Massive ECS publishes a bitset core without a UnityEngine reference, immediate structural operations, compact snapshot creation, and rollback-oriented facilities. StaticEcs publishes a hierarchical inverted bitmap design, fixed entity slots, SoA access, batch operations, parallel queries, snapshots, and Unity Burst integration.

ABC applies compatible lessons where they preserve its product promise:

- concrete types receive cached integer IDs;
- actors store only the compact module entries they own;
- world indexes are lazy and packed;
- multi-data queries begin with the smallest index;
- structural mutation is safe during iteration;
- struct query actions avoid boxing and delegate dispatch.

ABC does not claim SoA locality, Burst execution, unmanaged component density, rollback snapshots, or million-entity parity. Choosing reference-oriented data preserves stable object identity, interfaces, managed collections, Unity references, and direct dependency caching. A specialized ECS remains the correct answer when those data-oriented capabilities dominate the workload.

## Measured ABC baseline

The repository's current Editor/Mono regression shape reports:

| Metric | Unity 2022.3.62f2 | Unity 6.0.71f1 |
| --- | ---: | ---: |
| One-data indexed query | 1.40 ns/model | 1.33 ns/model |
| Two-data indexed intersection | 7.86 ns/model | 7.36 ns/model |
| Exact data access | 13.22 ns/op | 11.83 ns/op |
| Command dispatch | 10.80 ns/op | 9.67 ns/op |
| World behaviour dispatch | 10.04 ns/model | 8.92 ns/model |
| Warmed hot-path `GC.Alloc` samples | 0 | 0 |
| Empty model retained footprint | 94.82 B | 91.75 B |

Timing rows are five-sample medians from one development machine. They are not presented as cross-framework wins because the repositories use different data layouts, workloads, Unity versions, and measurement methods. See [Performance methodology](Performance.md) for the exact test shapes and limits.

## Product advantage to protect

ABC should win on continuity:

1. A prototype starts as readable classes or MonoBehaviours.
2. The same modules move to scene-free `ActorModel` instances.
3. An `ActorWorld` adds ownership and mutation-safe ticking.
4. Only batched mechanics move to lazy indexed queries.
5. A measured hot query can switch from a delegate to a struct action without changing storage.

No architectural rewrite is required between those stages. Every future feature should strengthen that path or remain outside the core.
