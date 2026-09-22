# ABC Agent Contract

## Mission

ABC is an AI-ready Unity gameplay framework for fast prototyping that can scale into allocation-free indexed simulation without an ECS rewrite. Keep the human authoring experience obvious, keep generated code ordinary and editable, and keep runtime machinery compact, deterministic, and hidden.

The package must remain free, self-contained, and usable without Odin Inspector, source-generator DLLs, analyzers, or runtime dependencies.

## Read First

Use the smallest route that matches the task:

| Need | Read |
| --- | --- |
| Public API or lifecycle | `Scripts/Runtime/Core/Contracts`, `Actor.cs`, `ActorModel.cs` |
| Composition or lookup | `ActorModuleStore*`, `ActorModuleMap.cs`, `ActorCompositionExtensions.cs` |
| Commands | `ActorCommandRegistry.cs`, `Scripts/Runtime/Core/Command` |
| Large model sets | `ActorWorld.cs`, `ActorWorld.Queries.cs`, `ActorWorldQuery.cs` |
| Networking or server physics | `Documentation~/Networking.md`, `Scripts/Runtime/Core/Simulation`, `Scripts/Runtime/Core/Networking`, `Samples~/NetworkSimulation` |
| Bandwidth or online server capacity | `Documentation~/ServerScaling.md`, sample snapshot codec and its tests |
| Unity authoring | `Scripts/Editor`, `Documentation~/EditorTooling.md` |
| Usage examples | `Documentation~/GettingStarted.md`, `Samples~/Basic`, `Samples~/BotArena` |
| Performance evidence | `Tests/Editor/ActorPerformanceTests.cs`, `Documentation~/Performance.md` |

Do not load the whole repository when one route is enough.

## Architecture

- `Actor` is the GameObject-backed composition root.
- `ActorModel` is the sealed scene-free composition root.
- `ActorWorld` owns `ActorModel` lifetime, update phases, and lazy exact-type indexes.
- `IActorData` holds state.
- `IActorBehaviour` holds logic and receives one `Owner`.
- `IActorTick`, `IActorFixedTick`, and `IActorLateTick` opt behaviours into update phases.
- `IActorCommand` and `IActorCommandListener<T>` provide local typed messages.
- `ActorBlueprint` provides reusable Unity-authored composition.
- `ActorRegistry` is the read-only lookup facade for initialized scene `Actor` instances.
- `ActorWorldRunner` is an optional scene host; worlds are not global singletons.
- `ActorSimulation` runs externally clocked FixedTick and an optional caller-owned physics step, not rendering or transport.
- `ActorNetworkMap` binds explicit nonzero `ActorNetworkId` values to initialized actors within one session.

Use `Actor` for Transform, physics, animation, and inspector-authored objects. Use `ActorModel` for scene-free gameplay and tests. Add `ActorWorld` when ownership, batch updates, or indexed queries are useful. Do not introduce a second component model for scale.

## Minimal Runtime Pattern

```csharp
using Abc.Unity;

var actor = new ActorModel("Enemy")
    .WithData(new HealthData())
    .WithBehaviour(new DamageBehaviour());

using var world = new ActorWorld("Combat", 1000);
world.Add(actor);
world.Query<HealthData>().For(static (model, health) => health.Regenerate());
```

Generated gameplay code uses one runtime namespace: `Abc.Unity`.

## Runtime Invariants

- One concrete data type and one concrete behaviour type may exist per actor.
- One module instance may belong to only one actor.
- Registration ownership uses the weak-key module table; a behaviour's mutable `Owner` alone is not an ownership check. Release ownership only when the module's final role is removed.
- All modules finish `PreInitialize` before any module begins `Initialize`.
- Initialization failure rolls back newly added modules and ownership.
- Cleanup is reverse-order, idempotent, and safe after partial initialization.
- Destroyed actors cannot be initialized or mutated again.
- Nested scene actors own their own descendant modules.
- Tick, command, and query mutation must not skip, repeat, or corrupt surviving work.
- `ActorWorld` membership and query indexes must remain consistent after add, remove, clear, self-destruction, and tag or data changes.
- Polymorphic module access succeeds only for one unambiguous match.
- World queries use exact concrete data types.
- Game tags use stable explicit `ActorTag` integer IDs; never derive persisted IDs from runtime string hashes.

Preserve these invariants transactionally. Never leave half-registered modules, listeners, tickables, ownership, or query entries after an exception.

## Networking Boundary

For replication, prefer the selected SDK's existing observer, delta and send-budget facilities. The optional sample codec is not a core replication engine. Capture/quantize shared state once per replication tick; never recapture the entire world separately for each recipient. Only delta against authenticated, receiver-acknowledged immutable baselines. Bound per-peer history and queues, keep session epochs explicit, and recover missing baselines with full state. Do not equate zero allocations or byte-count examples with proven player capacity; require end-to-end load evidence.

`Documentation~/Networking.md` has the SDK-specific boundary matrix for Mirror, Photon Fusion/PUN/Quantum, PurrNet, ENet-CSharp, FishNet, LiteNetLib and Unity Netcode. A listed integration target means the ABC simulation API can participate through a game-owned adapter; it does not imply a packaged adapter or tested SDK-version compatibility. Never tick an ABC stateful world in a framework rollback/resimulation callback unless its state can be restored and replayed. Quantum authoritative logic stays in Quantum; ABC can provide views and tools around it.

- Keep network SDK types, serializers and wire schemas in game-owned adapter assemblies, not gameplay modules or the ABC runtime.
- Use exactly one authoritative clock per world/actor and one physics driver per physics scene. Disable `AutomaticUpdates` before an external clock takes over.
- Put authoritative logic in `IActorFixedTick` when using `ActorSimulation`; it does not run Update/LateUpdate behaviours.
- Keep world, actor and Unity physics mutations on the simulation/main thread. Background receivers enqueue bounded input only.
- Validate authenticated ownership, schema/length, values, sequence and work budgets before local command dispatch. `SendCommand` is not an RPC or permission check.
- Use explicit network IDs with a session epoch and spawn generation where required. Never serialize type IDs, world slots, hashes, instance IDs or actor object graphs.
- Capture explicit numeric snapshots after physics using caller-owned buffers. Transport, replication, prediction, reconnect and rollback are separate adapter responsibilities.
- Do not silently change global physics/time settings or promise cross-platform determinism. A faulted simulation is not a rolled-back simulation.
- The network identity map is a deliberately public narrow boundary API; its mutable dictionaries and bindings remain private.

## Performance Contract

The warmed hot paths are module access, tick dispatch, command dispatch, and world queries.

- Allocate no garbage in warmed hot paths.
- Do not use LINQ, iterator blocks, boxing, transient collections, string keys, or repeated `Type` hashing there.
- Reflect only on cold registration paths and cache the result by concrete type.
- Keep registries and indexes lazy so unused features cost no per-model memory.
- Prefer compact arrays and integer type IDs for small compositions.
- Cache behaviour dependencies during `Initialize`; do not look them up every tick.
- Use struct query actions for maximum throughput. Delegate queries remain the prototype-friendly path.
- Measure changes with `ActorPerformanceTests`; never claim a speedup from intuition.

Do not add Burst, Jobs, Entities, unsafe code, pooling, or code generation unless a benchmark and a real workload justify the complexity.

## Public API Policy

Default classes and implementation helpers to `internal`. A type is `public` only when game code must construct, implement, inherit, serialize, or call it. Keep editor windows, inspectors, mutable registries, storage, diagnostics, schedulers, and utility collections internal.

- Seal concrete public classes unless inheritance is a deliberate extension point.
- Expose read-only state and narrow mutation methods.
- Do not expose mutable internal collections.
- Do not add service locators or new global singletons.
- Do not add aliases or compatibility members before a released API requires them.
- Breaking public API changes require changelog and documentation updates.

Assembly names and package identity stay lowercase for compatibility. C# namespaces are PascalCase:

- Runtime: `Abc.Unity`
- Editor: `Abc.Unity.Editor`
- Tests: `Abc.Unity.Tests`
- Basic sample: `Abc.Unity.Samples.Basic`
- Bot Arena sample: `Abc.Unity.Samples.BotArena`
- Network Simulation sample: `Abc.Unity.Samples.NetworkSimulation`

## Code Standard

`.editorconfig` is authoritative.

- Types, methods, properties, events, constants, and static readonly fields use PascalCase.
- Interfaces use an `I` prefix.
- Private instance fields use `_camelCase`.
- Parameters, locals, and local constants use camelCase.
- Use explicit accessibility and block-scoped namespaces.
- Keep control flow direct and dependencies explicit.
- Prefer 5–25 line methods; reconsider methods above 30–40 lines.
- Keep a source file below 400 lines. Split by responsibility, not arbitrary regions.
- Use one main type per file.
- Source comments, block comments, TODO markers, and XML documentation comments are forbidden. Make code self-explanatory and put durable explanation in `Documentation~`.
- Do not create generic `Manager`, `Helper`, or `Utility` abstractions without one precise responsibility.

## Editor and Generated Code

Editor tooling must work in both Unity Personal and Pro skins, use `SerializedProperty`, support Undo, validate before Play Mode, and stay inside `abc.unity.editor`.

Use `Tools > ABC > Feature Scaffold` for routine feature boilerplate. Generated source must be deterministic, readable, compilable, editable, and safe to commit. It must never overwrite an existing file and must add zero runtime generator cost.

For headless scaffolding use `-executeMethod Abc.Unity.Editor.ActorFeatureScaffold.Generate -abcFeature <Name> -abcNamespace <Namespace> -abcOutput Assets/<Folder>`. Read `Documentation~/AIReady.md` for the smallest adoption workflow. Do not add compatibility shims or a second component architecture. Keep existing scene assets and gameplay behavior unless the requested feature requires a change.

When changing editor UI:

- use native UI Toolkit controls and shared USS styling; do not add IMGUI, IMGUIContainer wrappers, or ReorderableList;
- preserve keyboard and narrow-window usability;
- use Unity-native controls and clear hierarchy;
- show actionable validation beside the affected workflow;
- keep editor APIs out of player assemblies;
- update `Documentation~/EditorTooling.md` when the workflow changes.

Do not assume a gameplay folder layout. Use the host project's existing folders and assembly definitions. Scaffold defaults are stored in `ProjectSettings/ABC.asset`; explicit `-abcOutput` and `-abcNamespace` arguments override them. Source output stays under the host Unity project's `Assets` directory. Keep asset references serialized or GUID-based instead of loading sample assets through fixed paths or `Resources` names. Use `Tools > ABC > World Explorer` to inspect worlds, modules, and runtime values before adding custom diagnostics.

## Asset Safety

- Never delete or regenerate `.meta` files for surviving assets.
- Move Unity assets with their `.meta` files.
- Give every new imported asset a unique stable GUID.
- Do not hand-edit serialized scenes, prefabs, or assets unless their structure is fully understood.
- Preserve user changes and unrelated dirty files.

## Validation

Run the fast repository gate first:

```powershell
pwsh -File Tools~/validate.ps1
```

Then validate proportionally:

1. Run focused EditMode tests for the changed subsystem.
2. Run the full `abc.unity.tests` EditMode suite.
3. Compile the imported Basic, Bot Arena and Network Simulation samples after public API or assembly changes.
4. Build a player after runtime, assembly, or serialization changes.
5. Run performance tests after storage, dispatch, lookup, or query changes.
6. Check the Console for new warnings as well as errors.

Bot Arena and Network Simulation include optional test assemblies guarded by the installed Test Framework package. Verify that importing the samples also compiles in a project without that package. Network Simulation tests run in PlayMode, not EditMode, because runtime local physics scenes require Play Mode. For networking changes, run those tests and the headless physics player smoke check on each validated Windows Mono version. Named networking SDK adapters, optimized Dedicated Server targets and wire-level multiplayer tests need separate evidence.

The package minimum is Unity 2022.3. The agreed release scope is Unity 2022.3 LTS and Unity 6 on Windows with Windows x64 Mono players. Do not imply that untested platforms or IL2CPP are certified.

## Evidence and Positioning

Keep comparison claims factual and reproducible. Compare workflow and architecture directly from competitors' published documentation. Compare performance only with the same workload, build mode, backend, hardware, warmup, and measurement method. Label local numbers as local measurements, not universal rankings.

ABC wins by combining low setup cost, one readable composition model, built-in free tooling, and selective indexed scale. Do not claim that it replaces data-oriented ECS for every workload.

## Definition of Done

A change is done only when:

- lifecycle and ownership remain correct on success, mutation, failure, and cleanup paths;
- public surface and serialization changes are intentional;
- warmed hot paths retain their allocation contract;
- new files follow namespace, assembly, naming, size, and asset rules;
- tests cover the regression or invariant;
- documentation and sample code match the shipped API;
- the fast gate and applicable Unity validation pass without new warnings.
