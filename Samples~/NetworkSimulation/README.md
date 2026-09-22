# Network Simulation

A small authoritative simulation boundary, not a replacement for your networking library. It runs Unity 3D physics in an isolated scene, without cameras, renderers, audio, assets, or input-device dependencies. The same session works in a graphical Unity player or a desktop headless player.

## Try it in the Editor

1. Import **Network Simulation** from ABC's Package Manager samples.
2. In an empty scene, use `GameObject → ABC Samples → Server Physics Host`.
3. Enter Play Mode and open `Tools → ABC → World Explorer`.
4. Select **Network Server**. Inspect its model, input sequence, movement, and body data. The Game view intentionally has no presentation; the Scene view shows the physics collider gizmos.

The host supplies one local test input per fixed step. Replace that source with your transport adapter; the physics and gameplay session do not need to change. The host simulates at 60 Hz and captures quantized snapshots at 20 Hz, round-tripping them locally through the delta codec. No sockets or real clients are created.

## Small adapter boundary

```csharp
using Abc.Unity;
using Abc.Unity.Samples.NetworkSimulation;
using UnityEngine;

using var session = new PhysicsServerSession(1f / 60f);
var id = new ActorNetworkId(1);
session.Spawn(id, authenticatedPeerId: 7, Vector3.up * 4f);
var snapshots = new ServerSnapshot[128];

var input = new ServerInput(id, sequence: 1, Vector2.right);
session.TryApplyInput(authenticatedPeerId: 7, in input);
session.Simulation.Step();
var count = session.CopySnapshots(snapshots);
```

The adapter serializes the first `count` snapshots into its own reusable send buffer. It derives the peer ID from the authenticated connection, never from the packet. DTOs contain explicit IDs and numeric state; no Actor, Rigidbody or Unity object references cross the wire.

`TryApplyInput` rejects unknown actors, wrong owners, non-finite or oversized movement, stale/duplicate sequences, jumps beyond 1,024 sequences, and more than one accepted input per actor per simulation step. Input starts at sequence 1, is held for at most six simulation steps, and then expires to zero movement. This deliberately simple latest-intent policy is not a prediction/replay input history. Coalesce received movement to the newest valid command before submitting once per tick; do not use it unchanged for discrete actions such as firing or purchases.

Snapshots acknowledge only inputs applied by the fixed behaviour. Read them after `Step`, not halfway through input processing. `Despawn` tears down the model, identity binding, and body. The adapter must emit the corresponding reliable despawn message. Do not reuse an ID during a connection epoch. Reconnects need a new epoch and cleared receive/snapshot buffers.

## Optional compact snapshots

`QuantizedServerSnapshot.TryCreate` converts a numeric snapshot into immutable centimeter-resolution wire state. `ServerSnapshotCodec.TryWrite` takes that state, a recipient-acknowledged baseline, and a caller-owned byte buffer. Pass `default` as the baseline for a full record. Reserve `ServerSnapshotCodec.MaxPacketBytes` bytes once; send only the returned length. Invalid state or an insufficient buffer fails without a partial record.

On receipt, use `TryGetBaseline` as a lookup hint into bounded history, then `TryRead` to validate/decode the complete record. Missing baselines need full resynchronization, not a best-effort application to the latest state. Keep immutable quantized state as the baseline rather than converting it back to floats and quantizing again. The codec does not manage connections, ACKs, history, interest management or freshness. Your authenticated session envelope must protect epochs and lifecycle ordering.

Small-ID test vectors encode 22-byte full records, 7-byte movement/input deltas and 5-byte unchanged-state records. Larger IDs/counters and changes require more bytes, with a 49-byte bound. Position is limited to +/-10 km per axis relative to the current origin and velocity to +/-327.67 m/s. Out-of-range and non-finite values fail; the authoritative physics state remains unquantized. See the package's `Documentation~/ServerScaling.md` for the format, receiver-history rules, traffic arithmetic and capacity limitations.

The built-in loopback immediately confirms successful decoding and therefore reuses the received state as its next baseline. A real sender must wait for an authenticated recipient ACK. It cannot promote the last packet merely because it was sent. Use your networking SDK's replication instead of this sample codec when the SDK already provides the required behavior.

## Headless player check

The batch-only editor helper creates a temporary empty scene, builds a Windows x64 Mono player, then removes that temporary scene. It does not require a particular gameplay directory or modify the project's build-scene list. Close the project in the Editor before running the command and choose a fresh build directory:

The ABC core stays independent from Photon, Mirror, PurrNet, ENet-CSharp, FishNet, LiteNetLib and Unity Netcode. The sample session can be called by a server-owned adapter; it does not provide SDK-specific prefabs or adapters. Read `Documentation~/Networking.md` before connecting a tick, authority callback, object identity or physics scene. Prediction/rollback systems need their own simulation state inside their rollback model; the sample's `ActorSimulation` cannot replay ABC state.

The helper leaves project graphics settings untouched and restores the scripting backend even if the build fails. The standalone batch host caps its frame rate at 60 to avoid a busy-wait render loop; the session itself never changes application timing. The Editor build command keeps graphics enabled to avoid Unity 2022.3 ambient-probe bake warnings; the resulting player runs without graphics.

```text
Unity.exe -batchmode -quit -projectPath <project> -executeMethod Abc.Unity.Samples.NetworkSimulation.Editor.NetworkSimulationBuild.BuildWindowsMono -abcBuildPath <empty-build-directory> -logFile <build-log>
```

Run the resulting player:

```text
"<build-directory>/ABC Server.exe" -batchmode -nographics -abcServerSmoke -logFile <player-log>
```

The smoke mode runs 300 fixed steps, verifies floor collision, movement, input acknowledgement and rejection of a different peer, logs `ABC_SERVER_SMOKE_PASSED`, and exits with code 0. It also logs `ABC_SNAPSHOT_CODEC_PASSED` after 100 local codec round trips, including encoded bytes versus full-record bytes. This synchronous loopback excludes network overhead, ACK latency and packet loss. A failed assertion exits with code 1. Without `-abcServerSmoke`, the host keeps simulating until the player is stopped.

This validates desktop headless execution, not the separately optimized Dedicated Server build target. FishNet/LiteNetLib assemblies are neither required nor installed by this sample. The snapshot codec is an editable schema example, not a general replication system. Real sockets, authentication, encryption, connection lifetime, latency, packet loss, prediction and reconnect behavior belong to the selected adapter and require its own integration tests.

The tested Unity 2022.3 desktop headless player reports unsupported built-in `Sprites/Default` and `Sprites/Mask` shaders with its Null graphics device during engine startup. Its physics checks still pass and it exits with code 0; the Unity 6 run has no such messages. This is not a renderer used by the sample. Do not confuse those specific startup messages with gameplay exceptions, and do not suppress arbitrary errors in CI.

## Files to read

| File | Responsibility |
| --- | --- |
| `PhysicsServerSession` | Ownership, input validation, isolated physics, snapshot extraction |
| `ServerBodyData` | Server-owned state and body lifetime |
| `ServerMoveBehaviour` | Cached fixed-step movement |
| `ServerInput` / `ServerSnapshot` | Explicit transport-neutral DTOs |
| `QuantizedServerSnapshot` | Validated immutable numeric wire state |
| `ServerSnapshotCodec` | Bounded full/delta encoding and baseline-checked decoding |
| `ServerPhysicsHost` | Separate simulation/snapshot pacing and headless loopback smoke check |

All world and physics access stays on Unity's main thread. Bound transport queues and packet lengths before calling the session. The sample validates gameplay intent but is not a complete security or denial-of-service boundary.

The optional sample tests run in **PlayMode**, because Unity creates local runtime physics scenes only while playing. The assembly is excluded when Test Framework is not installed.
