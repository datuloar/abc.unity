# Networking and server simulation

ABC exposes network integration points: gameplay can be driven by an external clock, mapped to stable network identities, and simulated without presentation. The core does not depend on FishNet, LiteNetLib, Unity Netcode, Mirror, Photon, ENet-CSharp, PurrNet, or a serializer. Keep the networking library in a game-owned adapter assembly referencing `abc.unity`.

**Network-ready means a tested simulation boundary, not an included multiplayer stack.** ABC does not supply RPCs, packet delivery, automatic replication, matchmaking, prediction, rollback, or authentication.

## Ownership at a glance

```text
FishNet / Photon / Mirror / PurrNet / ENet-CSharp
                  |
      bounded decode + authenticated peer
                  |
       game-owned authority/input adapter
                  |
       ActorSimulation.Step on main thread
          |                       |
   ActorWorld.FixedTick     optional physics step
                  |
         explicit numeric snapshots
                  |
       serializer + network delivery
```

| Responsibility | Owner |
| --- | --- |
| Data, behaviours, commands, queries, cleanup | ABC |
| Fixed step and local completed-step counter | `ActorSimulation` |
| Session-local identity lookup and destruction cleanup | `ActorNetworkMap` |
| Clock, ordering, authority, wire protocol, replication | Game/network adapter |
| Physics scene and exactly one simulation driver | Game or networking framework |
| Interpolation, prediction, rollback, interest management | Selected netcode and game |

## One clock per simulation

Put authoritative work in `IActorFixedTick`. Create a world and fixed step once:

```csharp
using var world = new ActorWorld("Server", 4096);
var simulation = new ActorSimulation(world, 1f / 60f);
```

At each server tick: drain validated input, call `simulation.Step()`, then capture output. `Step` calls only `World.FixedTick(DeltaTime)`, optionally calls the physics delegate supplied at construction, then increments `Tick`. Tick zero means no completed steps. It is a local `ulong` counter, not a synchronized timestamp or a prediction API. Use the networking framework's tick labels when encoding its packets.

Render `Tick`/`LateTick` work stays explicitly outside this authoritative step. ABC reads no wall clock or `Time.deltaTime` here. It does not accumulate time, drop ticks, alter `Time.timeScale`, or control network pacing. The standalone sample bounds catch-up work to eight steps per frame and retains backlog; a production server must define its overload/disconnect policy.

For an existing scene host:

```csharp
runner.AutomaticUpdates = false;
var simulation = new ActorSimulation(runner.GetOrCreateWorld(), 1f / 60f);
```

For a scene-backed Actor driven directly by netcode:

```csharp
actor.AutomaticUpdates = false;
actor.FixedTick(networkDeltaTime);
```

These switches disable all automatic ABC update phases, not initialization or lifetime. Manual calls remain available while the actor is initialized, alive, active and enabled. Author the switch off in the inspector or set it during network setup, before the first tick. Do not disable the Actor component as a substitute: disabled scene Actors cannot tick manually either. Neither switch disables another MonoBehaviour's `FixedUpdate`, Unity auto physics, or third-party scheduling.

Nested `ActorSimulation.Step` calls are rejected. If an exception escapes the step or its physics callback, the simulation becomes faulted and refuses retries of partially applied work; recreate/resynchronize the session. This is not transactional rollback. Existing ABC behaviour/command exceptions are still logged and isolated by dispatch, so production servers also need an application-level fault policy for those errors. `ActorSimulation` does not own or dispose its world.

## Stable IDs, not storage indexes

```csharp
using var identities = new ActorNetworkMap(4096);
var playerId = new ActorNetworkId(42);
world.Add(player);
identities.Bind(playerId, player);

if (identities.TryGetActor(playerId, out var actor))
    actor.SendCommand(validatedCommand);
```

The server assigns IDs. `ActorNetworkId.Value` is an explicit nonzero `ulong`; zero/default is invalid. Serialize the value using your protocol's defined encoding. A map supports initialized `Actor` and `ActorModel` instances, enforces one ID per actor and one actor per ID, and removes bindings on destruction. Separate sessions can use the same numeric IDs. `Unbind`, `Clear` and `Dispose` release bindings without destroying actors; removing a live model from a world does not implicitly unbind it.

Do not recycle IDs while packets from the old spawn can still arrive. Include a connection/session epoch and, if your network library reuses object IDs, a spawn generation in the adapter protocol. ABC does not invent a wire protocol or protect against stale epochs for you. Never send `WorldIndex`, type-registration IDs, `GetInstanceID`, actor names, hash codes, or object references as network identity. ABC module/command IDs are process-local and registration-order dependent.

Maps are optional, not global, and add no storage to actors that do not use them. Binding is cold work with dictionary/delegate storage. Warmed ID lookup and fixed-step dispatch have an allocation regression test; serializers and network libraries must be measured separately.

## Authoritative input and snapshots

The imported **Network Simulation** sample provides `PhysicsServerSession`, `ServerInput` and `ServerSnapshot`. It checks the authenticated peer, known actor, monotonically increasing sequence with a bounded forward window, finite normalized movement and per-step input budget. Stale input expires instead of moving forever after a disconnect. Snapshot extraction writes to a caller-owned array and reports only the last applied input sequence.

This is an example policy for continuous movement, not a generic secure command gateway. Your adapter must also:

- derive identity from the authenticated connection, not a peer ID supplied in payload;
- validate packet version, length, message kind, value ranges and session epoch before dispatch;
- bound queued bytes, messages per peer, spawns, and total work before touching gameplay;
- resolve ownership and permission for each command; `SendCommand` is local, not an RPC;
- serialize explicit DTO fields with stable schema IDs; never deserialize arbitrary CLR types or an Actor object graph;
- define reliable spawn/despawn/baseline delivery, stale-snapshot rejection, late join, reconnect and acknowledgement rules;
- keep client presentation separate from authoritative state and define interpolation/prediction explicitly.

Choose movement coalescing, reliable events, and input history according to gameplay. The sample's one-input-per-step rule is deliberately small; it is not a substitute for latency-tolerant prediction queues.

For transport-level optimization, the sample includes an optional quantized delta codec that writes into reusable buffers, verifies baseline identity/tick on decode, and falls back to full state when smaller. It does not choose recipients or maintain per-peer ACK history. Read [Server scale and bandwidth](ServerScaling.md) for its usage, limits, byte-count evidence, packet-loss recovery contract and production load-test checklist. Do not serialize the entire world to every peer at the physics tick rate.

## Server physics

The sample creates a scene with `LocalPhysicsMode.Physics3D` and advances its `PhysicsScene` explicitly. It creates Rigidbody/Collider objects only, retains gravity and contact resolution, and leaves the global physics mode and active scene unchanged. Separate sessions own separate physics scenes. [Unity's local physics API](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/PhysicsScene.Simulate.html) supports manually advancing the associated scene.

For an already owned 3D physics scene, cache the delegate at setup:

```csharp
var simulation = new ActorSimulation(world, 1f / 60f, physicsScene.Simulate);
```

For 2D, use an independently owned `PhysicsScene2D` and an adapter that checks its result:

```csharp
var simulation = new ActorSimulation(world, 1f / 60f, deltaTime =>
{
    if (!physicsScene2D.Simulate(deltaTime))
        throw new InvalidOperationException("The 2D physics step did not run.");
});
```

These variables refer to caller-owned valid local scenes. Only manually advance a default scene after explicitly arranging its simulation mode and lifetime in your adapter. If FishNet or another framework owns physics, omit the physics delegate and use that framework's post-physics hook to capture snapshots. Never run the same scene through both clocks. Transform-based teleports/raycast workflows may also require explicit transform synchronization at the game boundary. Consult the [2D simulation contract](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/PhysicsScene2D.Simulate.html) for that engine.

Fixed time steps improve reproducibility; they do not promise cross-platform PhysX determinism, lockstep, rollback, or safe physics execution on a background thread. ABC worlds and Unity physics are single-threaded at this boundary. Marshal background transport callbacks through a bounded queue to the main thread; do not call Actor APIs from socket threads.

## FishNet adapter recipe

Use the installed FishNet version's `TimeManager` as the sole clock. In a non-predicted authoritative-world adapter, create `ActorSimulation` with `(float)timeManager.TickDelta`, run its step from server `OnTick`, and capture physics-dependent output in `OnPostTick` when Physics Mode is TimeManager. Unsubscribe on network stop and dispose session-owned maps/worlds. A host must not advance the authoritative world twice through its server and client paths. These hooks and physics ordering are documented in the [FishNet TimeManager API](https://fish-networking.com/FishNet/api/api/FishNet.Managing.Timing.TimeManager.html).

Do not pass a physics delegate when FishNet already owns simulation. For predicted objects, integrate gameplay with FishNet's prediction/reconcile lifecycle instead of additionally advancing it through ordinary ABC ticks. ABC's completed-step counter has no rewind operation and is not a drop-in prediction timeline. The recipe describes an integration boundary, not a shipped or version-certified FishNet adapter.

## LiteNetLib adapter recipe

Poll on the simulation thread, decode into bounded reusable input buffers, run fixed steps, then serialize snapshots into reused writers. If using unsynchronized/background callbacks, queue input instead of mutating ABC or physics from that callback. LiteNetLib provides transport and polling; your adapter owns the simulation clock, session identity, authority and replication. See its [official usage examples](https://github.com/RevenantX/LiteNetLib#usage-samples). Do not copy a blocking console `Thread.Sleep` loop into Unity's main thread. No LiteNetLib package is required by ABC itself.

## Integration readiness by framework

ABC's clock, identity map, main-thread boundary and numeric snapshots are usable from a game-owned adapter for each row below. **No SDK-specific adapter, sample assembly or end-to-end transport integration is shipped or tested.** Compile against the exact SDK version installed in the game; callback names, package licensing, build targets and platform support belong to that version.

| Framework/product | Integration fit | Main constraint |
| --- | --- | --- |
| Mirror | Strong for a dedicated authoritative server: step an `ActorWorld` on one server-owned scheduler, then let Mirror sync/spawn and interest systems deliver results. | Mirror's send rate is not an ABC simulation clock. Schedule simulation deliberately and don't also auto-tick scene Actors. Use Mirror SyncVars/messages and Interest Management when suitable instead of sending the sample's whole snapshot array. |
| Photon Fusion 2 | Strong for forward-only server-authoritative gameplay on the State Authority. A Fusion tick callback can feed validated input into a separate ABC world. | Fusion can predict and resimulate. `ActorSimulation.Step()` cannot restore/replay ABC state. Never advance the same ABC world through client prediction or resimulation. Let Fusion own physics if it owns that scene. Shared Authority is not a dedicated authoritative server. |
| Photon PUN 2 / Realtime | Usable for room/peer gameplay when the game explicitly chooses an authority peer and scopes simulation to it. PUN can serialize explicit DTO fields from a `PhotonView`; Realtime is a lower-level cloud communication API. | PUN's Master Client is a client role, not a dedicated server guarantee. Handle authority changes, room cleanup, late join and messages yourself. Do not treat RPCs or client-owned state as trusted server validation. |
| Photon Quantum | Use ABC for Unity presentation, views, tools, and non-authoritative UI around Quantum. | Quantum owns deterministic simulation, state, physics, input and rollback. ABC behaviours and Unity physics are not substitutes for Quantum's deterministic simulation model; do not also simulate authoritative gameplay in ABC. |
| PurrNet | For non-predicted server-owned gameplay, a PurrNet tick callback can drive a separate ABC world; use PurrNet identity, ownership and sync modules at the adapter boundary. | PurrDiction restores and replays predicted ticks. ABC has no state history/restore API, so its worlds cannot join that rollback path directly. Avoid advancing the same physics scene through both systems. |
| ENet-CSharp | Strong transport-level fit: poll ENet, decode messages, drive the fixed ABC simulation, and encode selected snapshots. | ENet supplies transport, channels and reliability; the game supplies authority, replication, serialization, interest management and clock. Poll `Host.Service(0, ...)` without blocking Unity; dispose received packets and bound queued work. |
| Unity Netcode for GameObjects / Entities | Adapter fit at the network/ghost lifecycle and simulation callbacks. | Use that package's authority, serializer, relevancy and tick rules. For Entities, keep ECS/Jobs-owned state in its native simulation; don't mirror every component through ABC. |

### Photon Fusion 2

For a non-predicted dedicated/server-authority path, call the ABC world from Fusion's forward `FixedUpdateNetwork` execution only when this peer has State Authority. Do not call `Step()` during Fusion resimulation. Keep the ABC server world separate from client-predicted state. If Fusion's physics add-on drives a scene, do not pass a second physics step to `ActorSimulation`; order snapshot capture after that scene's authoritative physics callback. See [Fusion's simulation callbacks](https://doc.photonengine.com/fusion/v2/concepts-and-patterns/network-simulation-loop), [controller and resimulation guidance](https://doc.photonengine.com/fusion/v2/concepts-and-patterns/networked-controller-code), and [network physics modes](https://doc.photonengine.com/fusion/v2/manual/physics/physics-overview).

### Photon PUN 2 and Realtime

In PUN 2, make the peer/authority policy explicit and use `PhotonView` serialization callbacks or RPCs to carry validated DTOs. `PhotonView` reliable delta compression can suppress unchanged values, but it does not turn a Master Client into dedicated server authority. Photon Realtime needs the game to build its simulation and object-replication layer. For a real dedicated authoritative game server on Photon, evaluate Fusion Server or Photon Server separately. See [Photon's PUN synchronization](https://doc.photonengine.com/pun/current/gameplay/synchronization-and-state), [send-rate and traffic guidance](https://doc.photonengine.com/pun/current/troubleshooting/analyzing-disconnects), and [Photon product topology descriptions](https://doc.photonengine.com/photon/current/photon-products).

### Photon Quantum

Quantum owns deterministic gameplay state and rollback. Keep ABC on the Unity view side: use Quantum entity views/events to update ABC scene Actors for presentation, or use ABC editor tooling for authoring/debugging. Send player input to Quantum's input pipeline. Do not use `ActorSimulation`, ABC float data, `Rigidbody`, or local ABC commands as a second authoritative/predicted Quantum state machine. See [Quantum's Unity/Simulation split](https://doc.photonengine.com/quantum/v3/video-tutorials/beginner-guide) and [simulation/view examples](https://doc.photonengine.com/quantum/current/technical-samples/quantum-essentials-sample/quantum-assets-essentials).

### Mirror

Use a `NetworkManager`/`NetworkBehaviour` lifecycle to create and dispose a server-owned `ActorWorld` and maintain its `ActorNetworkMap`. Drive that world once from an explicit server fixed-step host. If ABC scene Actors are also present on networked GameObjects, disable `AutomaticUpdates` before the server takes over their clock. Keep Mirror's `NetworkIdentity`/connection ownership as the transport identity authority; map its IDs within a session and add a reconnect/spawn epoch where required. Configure Mirror Interest Management and its send intervals for the world instead of serializing every actor to every connection. `NetworkTime` is a synchronized clock, not a replacement for deciding the simulation step schedule. See [Mirror Interest Management](https://mirror-networking.gitbook.io/docs/manual/interest-management), [SyncVar updates](https://mirror-networking.gitbook.io/docs/manual/guides/synchronization/syncvar-hooks), and [time synchronization](https://mirror-networking.gitbook.io/docs/manual/guides/time-sync).

### PurrNet

For an ordinary server-owned ABC world, have one adapter subscribe to PurrNet's installed-version tick and step the world only on its authoritative server execution. Use PurrNet's identity, ownership and sync APIs for network objects and values. When using PurrDiction, keep gameplay state that needs prediction and rollback inside its predicted identities; ABC's step counter cannot replay state. Do not independently call `PhysicsScene.Simulate` when PurrDiction owns the physics pass. See the official [PurrDiction tick flow](https://purrnet.dev/docs/client-side-prediction/flow), [prediction installation/physics ordering](https://purrnet.dev/docs/client-side-prediction/installation), and [prediction policies](https://purrnet.dev/docs/client-side-prediction/prediction-policies).

### ENet-CSharp

ENet-CSharp is a low-level transport, not a Unity replication framework. Initialize/deinitialize its library with the server lifecycle, poll the host non-blockingly with timeout zero, decode/copy bounded packet data, then submit it to ABC on the simulation thread. Select reliable/unreliable delivery by message purpose, batch bounded snapshots, and implement session authentication, stable DTO schemas, ID epochs and recipient filtering in the game adapter. Dispose every received packet on every branch. Keep Unity and Actor APIs off transport threads. The [upstream repository](https://github.com/nxrighthere/ENet-CSharp) distinguishes its .NET `ENet-CSharp` binaries from `ENet-Unity`'s Unity plugin packaging; verify the native library binaries for your target OS/architecture and Mono/IL2CPP backend. The repository's Unity poll example uses zero timeout inside a game loop.

## Shared adapter lifecycle

For these libraries, the adapter should follow the same narrow sequence:

1. On authoritative session start, create the world, simulation clock, per-session identity map and bounded queues.
2. On authenticated input receipt, validate source ownership and packet bounds, then queue a typed input for the next authoritative tick.
3. At one authoritative tick, drain a bounded amount of input and step the ABC world exactly once. Let the selected framework own rollback and physics where configured.
4. After authoritative physics, capture reusable DTO state once. Let the framework's replication and observer facilities choose recipients; use the sample codec only when the game adapter deliberately owns per-recipient acknowledged baselines.
5. On despawn/disconnect/shutdown, unsubscribe callbacks and clear actor mappings, packet histories, epochs and owned sessions in a defined order.

The actor map remains local to one ABC process/session. A framework's object ID may be converted to an explicit `ActorNetworkId` within that session; preserve the framework's own epoch/generation separately and never trust a numeric ID from packet payload without binding it to the authenticated sender.

## Headless execution and validation boundary

Import Network Simulation and follow its README to build and run the 300-step smoke check. A Windows Mono player launched with `-batchmode -nographics` runs without a graphics device. That is [desktop headless mode](https://docs.unity3d.com/2022.3/Documentation/Manual/desktop-headless-mode.html), not certification of the separately optimized Dedicated Server build target.

The package remains a Unity runtime; `ActorModel` being scene-free does not turn the assembly into a standalone .NET server library. Dedicated Server platform modules, Linux, IL2CPP, real multi-process clients, transport-specific prediction, packet loss/reordering and reconnect tests are separate integration gates. Frameworks in the integration matrix are not automatically tested or certified adapters.

Before shipping multiplayer, test your actual client/server build pair under latency, loss, duplicates, out-of-order delivery, reconnection and host shutdown. Verify both the wire protocol and gameplay authority, not only that the two projects compile.

### Recorded local checks

Unity 2022.3.62f2 and 6000.0.71f1 each passed 143 EditMode tests and 23 Network Simulation PlayMode tests, including full/delta codec allocation and malformed-packet checks. The separate Windows x64 Mono headless players completed 300 fixed steps, collided with the floor and acknowledged input 300, exiting with code 0. Each performed 100 local snapshot codec round trips totaling 866 record bytes versus 2,316 bytes when forced full. The sample also built and ran in a project without Test Framework with the same smoke result. All three player builds reported zero build warnings. These checks validate the simulation/codec boundary, not a named transport integration or server capacity.

Unity 2022.3's desktop headless run emits built-in `Sprites/Default` and `Sprites/Mask` unsupported-shader messages during Null-device startup, before the server session starts. The physics assertions pass; Unity 6 does not emit these messages in the same check. Keep this known engine-level diagnostic visible. A general zero-error-log certification of Unity 2022.3 desktop headless mode is not claimed.
