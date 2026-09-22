# AI-ready development

ABC treats coding agents as first-class contributors without making generated code a runtime dependency. The same readable source, tests, and editor workflows serve both people and agents.

## Small context surface

An agent can begin with three inputs:

1. `AGENTS.md` for architecture, invariants, performance rules, and completion criteria.
2. One focused runtime route from its Read First table.
3. The closest test or Basic sample.

The runtime uses one `Abc.Unity` namespace and one module model across scene actors, scene-free models, and indexed worlds. There is no separate ECS vocabulary or generated accessor API to recover before editing a mechanic.

## Deterministic scaffolding

Open `Tools → ABC → Feature Scaffold` and choose the parts of a feature:

- data;
- behaviour;
- command and listener;
- struct query action;
- Blueprint providers.

The tool derives every type and file name from one valid C# feature name. It previews the output, writes UTF-8 source with stable formatting, and refuses to overwrite an existing file. The result is normal C# with no retained relationship to the generator.

Generated behaviours cache selected data during initialization but do not register a no-op frame tick. Add `IActorTick`, `IActorFixedTick`, or `IActorLateTick` only for logic that actually needs that phase.

This is the preferred answer to repetitive provider and feature boilerplate. Do not add a build-time generator when a visible editor scaffold can produce the same code once.

## Agent workflow

For a new mechanic:

1. Decide whether it needs `Actor`, `ActorModel`, or both.
2. Define the smallest data contract.
3. Cache behaviour dependencies during `Initialize`.
4. Use a local typed command for actor-local intent.
5. Add an `ActorWorld` query only for genuinely batched work.
6. Prefer a struct query action in a measured hot loop.
7. Add a focused regression test before changing storage or lifecycle code.
8. Run `pwsh -File Tools~/validate.ps1`, then the applicable Unity tests.

The scaffold is optional. Agents may create the same files directly when operating without an active Unity Editor, provided the generated shape follows `AGENTS.md` and `.editorconfig`.

## Headless feature creation

An agent can use the same tested templates without opening a window:

```text
Unity.exe -batchmode -nographics -quit -projectPath <project> -executeMethod Abc.Unity.Editor.ActorFeatureScaffold.Generate -abcFeature Health -abcNamespace Game.Combat -abcOutput Assets/Game/Combat -logFile <log>
```

This creates data, behaviour, command, query action and both Blueprint providers. The editor window remains the selective path when fewer files are needed. Both entry points share validation and writing: existing source or metadata is never overwritten, traversal and linked output directories are rejected, and failed writes roll back only files created by that invocation. Compile the result after generation before treating it as ready.

`-abcNamespace` and `-abcOutput` may be omitted to use the host project's saved ABC defaults. Explicit arguments take precedence. Use the project's established source folder; `Assets/Game/Combat` above is only an example. In Unity, choose defaults through `Tools → ABC → Project Setup` or the Feature Scaffold's **Use Selected Folder** and **Save as Project Defaults** controls. Custom gameplay assemblies must reference `abc.unity`.

## Add ABC to an existing game

Start with one complete mechanic, not a project-wide rewrite. Keep Unity physics, animation and rendering at the scene boundary. Put its state in data, its logic in behaviours and its local intent in typed commands. Existing services remain explicit constructor dependencies; do not wrap them in a new service locator.

Give the agent this prompt:

```text
Read the ABC AGENTS.md contract and inspect only the current combat feature.
Move its state and logic onto the existing Actor/ActorModel contracts.
Keep prefabs, assets, input bindings and public gameplay behavior unchanged.
Use the smallest feature slice, no compatibility aliases or parallel framework.
Cache dependencies during Initialize, keep warmed updates allocation-free,
and add focused tests for combat behavior and teardown before expanding the scope.
Report the changed files, test results and any behavior intentionally changed.
```

Blueprints are optional authoring assets, not an older runtime architecture. Use fluent composition in tests and agent-authored prototypes; use Blueprint providers when designers need reusable inspector values. Both produce ordinary independent modules and enter the same lifecycle. Never pay Blueprint cloning or dependency lookup costs inside a per-frame loop.

## Prompt contract

A useful feature request states gameplay intent and scale, not framework plumbing:

```text
Create a damage-over-time mechanic for scene actors and ActorModel simulations.
Damage is actor-local, health is queryable, and 20,000 models must update without warmed allocations.
Add tests and use the existing ABC lifecycle and command contracts.
```

The agent should infer the normal ABC shape, explain only meaningful tradeoffs, and avoid introducing services or abstractions that the request does not need.

## Network feature prompt

```text
Read ABC's Networking.md and the Network Simulation sample.
Integrate the project's existing network library through a separate adapter assembly.
Keep gameplay modules independent of its APIs. Use one authoritative fixed clock,
stable network IDs with reconnect epochs, validated peer-owned input, and explicit DTOs.
Keep Unity physics on the main thread and give each scene exactly one physics driver.
Use caller-owned buffers, cache dependencies, and test stale/duplicate/malformed inputs.
Do not claim prediction, rollback or transport support that has not been integration-tested.
```

`ActorSimulation` runs fixed behaviours only. Do not silently generate authoritative movement as `IActorTick` when the requested host advances only fixed ticks. The current scaffold emits an Update behaviour; change it explicitly to `IActorFixedTick` for this workflow. Networking does not require new component types, generated replication registries, or game-wide reflection.

For large online games, also read [Server scale and bandwidth](ServerScaling.md). Reuse the chosen SDK's observer/delta features first. If a custom wire schema is necessary, the sample codec shows caller-buffer encoding with checked baselines and full fallback; it does not implement peer history or ACK delivery. Generate explicit per-peer budgets, authenticated epoch handling and loss/reconnect tests at the adapter boundary. Avoid per-peer full-world extraction, unbounded reliable queues and player-capacity claims based on microbenchmarks.

## Human readability

- Generated types use domain names rather than keys or numeric IDs.
- Runtime internals remain internal; game code sees only composition contracts.
- Source comments are not used as a substitute for clear design. Durable architecture explanation lives in Markdown documentation.
- Files stay small enough to review without loading unrelated subsystems.
- Public state is read-only unless mutation is part of the deliberate gameplay contract.

## Runtime cost

Agent support has no player-side service. `AGENTS.md`, documentation, validation scripts, and the Feature Scaffold are outside the runtime assembly. Generated providers are ordinary Blueprint authoring types; generated data and behaviours use the same runtime path as handwritten code.

The framework does not send source, project state, or telemetry to an external AI service.
