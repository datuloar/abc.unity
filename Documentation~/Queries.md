# High-performance queries

ActorWorld queries provide fast batch processing over concrete data classes while preserving normal ActorModel composition.

## Prototype API

```csharp
world.Query<HealthData>()
    .WithTag(ActorTag.Enemy)
    .OnlyAlive()
    .For(static (actor, health) => health.Value++);
```

A non-capturing static lambda is cached by the compiler. The delegate call remains in the per-model loop, making this form ideal when readability matters more than the final few nanoseconds.

## Struct-action API

```csharp
public struct RegenerateAction : IActorQueryAction<HealthData>
{
    public int Amount;
    public int Processed;

    public void Execute(ActorModel actor, HealthData health)
    {
        health.Value += Amount;
        Processed++;
    }
}
```

```csharp
var action = new RegenerateAction { Amount = 2 };
var processed = world.Query<HealthData>().For(ref action);
```

Passing the action by reference preserves its state. The generic constraint emits a constrained call for value types, avoiding interface boxing and delegate dispatch.

## Intersections

Queries support one, two, or three exact data types:

```csharp
var query = world.Query<PositionData, VelocityData>();
var action = new MoveAction { DeltaTime = deltaTime };
query.For(ref action);
```

```csharp
world.Query<PositionData, VelocityData, AccelerationData>()
    .For(static (actor, position, velocity, acceleration) =>
    {
        velocity.Value += acceleration.Value;
        position.Value += velocity.Value;
    });
```

ABC begins with the smallest participating type index. Every additional membership check is a world-slot array lookup.

## Filters

Query structs are immutable. Filter methods return a new lightweight value:

```csharp
var enemies = world.Query<HealthData>().WithTag(ActorTag.Enemy);
var activeEnemies = enemies.OnlyAlive();
var nonPlayers = world.Query<HealthData>().WithoutTag(ActorTag.Player);
```

`CandidateCount` reports the smallest underlying index size before tag or alive filtering. `For` returns the number of models actually processed.

## Structural changes

The following operations are safe inside a query action:

- destroying the current or another model;
- spawning a model into the same world;
- adding or removing queried data;
- changing tag or alive state;
- nesting another query.

Removals take effect for unvisited entries in the current query. Additions to a participating query index become visible after the outermost query finishes. Models spawned during a query join the world after iteration and are first visible on the next pass.

This gives predictable traversal without a separate command-buffer API.

## Exact types by design

World queries require concrete data types. The following remains valid for actor-local composition:

```csharp
var locomotion = actor.GetData<ILocomotionData>();
```

The following is rejected:

```csharp
world.Query<ILocomotionData>();
```

Polymorphic queries would require refreshing an unknown set of interface indexes whenever any concrete module changes. Requiring a concrete type keeps index updates bounded and query performance predictable.

## Performance checklist

- Create and cache a query outside the frame loop when practical.
- Use the delegate overload for ordinary gameplay and a struct action for verified hot paths.
- Reserve ActorWorld capacity for known population sizes.
- Keep per-model action code larger than the dispatch itself before pursuing micro-optimizations.
- Resolve actor-local dependencies once during module initialization.
- Avoid capturing lambdas in repeated calls.
- Measure the target player and scripting backend, not only Editor/Mono.

## Choosing a query or behaviour

Use an actor behaviour when logic naturally belongs to one actor, has private cached dependencies, or interacts heavily with its Unity view.

Use a world query when one operation processes many models with the same concrete data composition. It is valid to use both: behaviours can handle events and state transitions while a query performs a high-volume numeric step.
