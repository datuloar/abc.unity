# Getting started

This guide builds the smallest complete ABC simulation and then shows how to place the same modules on a Unity GameObject.

For a compile-ready starting slice, open `Tools → ABC → Feature Scaffold`. It can generate data, behaviour, command, query action, and Blueprint providers as ordinary editable source files.

## Define data

Actor data is a reference type with stable identity. It can expose fields, properties, or narrower interfaces according to the needs of the game.

```csharp
using Abc.Unity;
using UnityEngine;

public sealed class PositionData : IActorData
{
    public Vector3 Value;
}

public sealed class MovementData : IActorData
{
    public Vector3 Direction;
    public float Speed;
}
```

## Define behaviour

Resolve dependencies during `Initialize`. The update method then performs normal field reads with no repeated lookup.

```csharp
using Abc.Unity;

public sealed class MovementBehaviour : IActorBehaviour, IActorTick
{
    private PositionData _position;
    private MovementData _movement;

    public IActor Owner { get; set; }

    public void Initialize()
    {
        _position = Owner.GetData<PositionData>();
        _movement = Owner.GetData<MovementData>();
    }

    public void Tick(float deltaTime)
    {
        _position.Value += _movement.Direction * (_movement.Speed * deltaTime);
    }
}
```

## Run without a scene

```csharp
using var world = new ActorWorld("Gameplay", 1024);

var actor = new ActorModel("Player", ActorTag.Player)
    .WithData(new PositionData())
    .WithData(new MovementData { Speed = 5f })
    .WithBehaviour(new MovementBehaviour());

world.Add(actor);
world.Tick(1f / 60f);
```

`ActorWorld.Add` initializes the actor. Disposing the world destroys every owned model and cleans its modules.

If Unity should own the loop, add `ABC/Actor World Runner` to a GameObject. It creates the world in Awake, forwards the enabled Update phases, and disposes it with the GameObject. Keep a serialized reference to the runner instead of adding a global world singleton.

## Run on a GameObject

1. Choose `GameObject → ABC → Actor`.
2. Add MonoBehaviour implementations of `IActorData` or `IActorBehaviour` to the object or its children.
3. Keep `Initialize On Awake` enabled for the normal scene lifecycle.
4. Use the runtime card in the Actor inspector to inspect state, module counts, and Kill/Revive behaviour.

A nested GameObject with its own `Actor` is an ownership boundary. The parent never collects modules below it.

## Compose with blueprints

Create an asset through `Assets → Create → ABC → Blueprints → Actor`. Use the Data and Behaviours add menus to create embedded providers. Providers are stored as sub-assets, support Undo, and are cloned for every actor instance.

Data and behaviour prototypes use `ICloneable` when available and otherwise clone through Unity serialization. Normal serializable classes need only a public parameterless constructor; no manual clone method is required.

## Send a command

```csharp
public readonly struct JumpCommand : IActorCommand
{
}

public sealed class JumpBehaviour : IActorBehaviour, IActorCommandListener<JumpCommand>
{
    public IActor Owner { get; set; }

    public void ReactActorCommand(JumpCommand command)
    {
    }
}
```

```csharp
actor.SendCommand<JumpCommand>();
```

Commands remain inside one actor. Use explicit game services or a dedicated event bus when communication has world or application scope.

## Define game tags

Built-in tags cover common examples. Game code can define compact custom values without changing the package:

```csharp
public static class GameTags
{
    public static readonly ActorTag Boss = new ActorTag(1001);
    public static readonly ActorTag Projectile = new ActorTag(1002);
}
```

Custom tags work with `SetTag`, `ActorRegistry`, `WithTag`, `WithoutTag`, and the Actor inspector. Keep IDs stable after content ships.

## Move a mechanic to a query

When the number of actors grows, move only the data-parallel mechanic:

```csharp
world.Query<PositionData, MovementData>()
    .OnlyAlive()
    .For(ref deltaTimeAction);
```

See [High-performance queries](Queries.md) for the delegate and struct-action forms.

## Recommended boundaries

- Use `Actor` for presentation, physics, animation, input, and scene references.
- Use `ActorModel` for simulation state, AI, combat, inventory, and tests.
- Cache module dependencies during initialization.
- Use commands for actor-local intent, not as a global service locator.
- Reserve world capacity when the expected model count is known.
- Add a query only after a batch operation becomes meaningful.
