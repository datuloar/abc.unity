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

## Prompt contract

A useful feature request states gameplay intent and scale, not framework plumbing:

```text
Create a damage-over-time mechanic for scene actors and ActorModel simulations.
Damage is actor-local, health is queryable, and 20,000 models must update without warmed allocations.
Add tests and use the existing ABC lifecycle and command contracts.
```

The agent should infer the normal ABC shape, explain only meaningful tradeoffs, and avoid introducing services or abstractions that the request does not need.

## Human readability

- Generated types use domain names rather than keys or numeric IDs.
- Runtime internals remain internal; game code sees only composition contracts.
- Source comments are not used as a substitute for clear design. Durable architecture explanation lives in Markdown documentation.
- Files stay small enough to review without loading unrelated subsystems.
- Public state is read-only unless mutation is part of the deliberate gameplay contract.

## Runtime cost

Agent support has no player-side service. `AGENTS.md`, documentation, validation scripts, and the Feature Scaffold are outside the runtime assembly. Generated providers are ordinary Blueprint authoring types; generated data and behaviours use the same runtime path as handwritten code.

The framework does not send source, project state, or telemetry to an external AI service.
