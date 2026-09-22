# Server scale and bandwidth

ABC can host authoritative gameplay, but it is not an MMO backend or a complete replication engine. No player-capacity or hosting-price claim follows from an allocation-free query. Measure the game, networking stack, physics and deployment together.

## What scales, and where

| Concern | ABC contribution | Game/network responsibility |
| --- | --- | --- |
| Gameplay CPU | Cached module access, indexed queries, struct actions, explicit fixed steps | AI frequency, pathfinding budgets, active regions and expensive mechanics |
| Memory and GC | Lazy runtime indexes and warmed allocation regressions | Connection history, packet buffers, persistence, SDK allocations and capacity limits |
| Network traffic | Explicit DTO boundary; optional sample quantization/delta codec | Observers, per-client baselines, pacing, batching, transport and encryption |
| Physics | Caller-owned local scenes with one simulation driver | Collision layers, body counts, sleeping, queries and the chosen controller model |
| Population growth | Independent worlds and sessions | Zone routing, multiple processes, handoff, persistence and service operations |

Registered accounts, concurrent users across a fleet, players in one zone, and visible actors per client are different capacity numbers. A large total audience can be spread across zones; thousands of mutually interacting players in one fight are a different workload. ABC does not automatically shard a world.

`ActorWorld` is a single-threaded managed composition boundary. It is not a packed struct/SoA store, does not compile behaviours with Burst, and must not be ticked concurrently from worker threads. Scene-free `ActorModel` instances avoid GameObjects, but the package still depends on Unity and is not a standalone .NET server library. Profile CPU-heavy numeric systems before deciding whether a separately owned Jobs/Burst/ECS subsystem is justified. More CPU cores do not automatically accelerate one ABC world.

## Reduce fan-out before compressing bytes

1. Send each client only the entities it may observe: nearby actors, its zone, party, quest state and authorized private data. Use the netcode library's observer system or a game-owned spatial index. Do not scan every actor separately for every peer each tick.
2. Run simulation and replication at independently chosen frequencies. The sample simulates at 60 Hz and extracts at 20 Hz; these are example settings, not an RPG recommendation. Capture and quantize shared state once per replication tick, then reuse it for recipients. `CopySnapshots` scans all sample bodies: calling it once per recipient multiplies extraction cost unnecessarily.
3. Replicate only meaningful fields at the required precision. Inventory changes, movement and cosmetic effects need different delivery policies. Static state can remain silent after acknowledgement; continuous motion needs interpolation or prediction and a freshness policy.
4. Use changed-field/delta encoding against receiver-confirmed state. Batch records into transport-sized packets and obey a per-peer byte budget, prioritizing important/recently starved entities. Preserve reliable lifecycle and transactional events separately from replaceable movement updates.
5. Bound history, queues and retries. Disconnect or degrade service deliberately when a peer cannot keep up. Never let reliable snapshot backlog or reconnect history grow without limit.

FishNet already provides changed-value synchronization and send-rate control through [SyncTypes](https://fish-networking.gitbook.io/docs/guides/features/network-communication/synchronizing), and recipient filtering through [NetworkObserver](https://fish-networking.gitbook.io/docs/fishnet-building-blocks/components/network-observer). Use these facilities when they fit the game instead of stacking a second replication system on top. A lower-level transport such as LiteNetLib leaves more of this policy to the application.

The same design principles appear in [Unity Netcode for Entities' snapshot guidance](https://docs.unity3d.com/Packages/com.unity.netcode@1.8/manual/optimization/limit-snapshot-size.html): send-rate limits, importance and bounded snapshot work/history. This is architectural guidance, not evidence that ABC matches that framework's parallel throughput.

## Optional sample delta codec

Import **Network Simulation** for `QuantizedServerSnapshot` and `ServerSnapshotCodec`. They belong to the sample, not `abc.unity`; adopt or replace the schema in the game adapter. FishNet users can instead use their SDK's serializers and replication.

```csharp
if (!QuantizedServerSnapshot.TryCreate(in snapshot, out var state))
    return;

if (ServerSnapshotCodec.TryWrite(in state, in acknowledgedBaseline, sendBuffer, out var length))
    transport.Send(sendBuffer.AsSpan(0, length));
```

`snapshot`, `acknowledgedBaseline`, `sendBuffer` and `transport` are adapter-owned values. The transport call is illustrative, not an ABC API. Allocate/reuse the buffer once; keep it valid until the transport has copied or completed sending it. Do not use `stackalloc` for a deferred/asynchronous send.

The sample format uses:

- fixed, explicit format bytes and numeric fields, with no reflection or CLR type names;
- centimeter position/velocity quantization, signed per-axis deltas and unsigned variable-length IDs/counters;
- one change-mask bit for each replicated field, with automatic full-record fallback when smaller;
- caller-owned `Span<byte>`/`ReadOnlySpan<byte>` buffers and no warmed codec allocations;
- exact baseline identity/tick checks, bounded parsing, overflow/range checks, and no partially decoded result on failure.

Position components must be within +/-10,000 meters relative to the game's current origin; velocity components within +/-327.67 meters/second. Invalid/non-finite values fail rather than silently clamp. Precision is 0.01 units before float reconstruction; do not quantize authoritative physics in place. Larger worlds need a zone/origin schema and consistent origin epochs, not globally imprecise float coordinates. Changing these limits or the schema requires a new protocol version and matching endpoints.

One record is at most 49 bytes. Tests assert these smaller example sizes with actor ID 7, ticks 100/101 and input sequences 100/101:

| Record | Encoded bytes |
| --- | ---: |
| Full quantized state | 22 |
| X position advances 5 cm and last-applied input advances by one | 7 |
| State unchanged except the new tick | 5 |

The sample wire layout is deterministic: `0x10` means full and `0x11` means delta. Both start with unsigned LEB128 actor ID and current tick. A full record continues with unsigned LEB128 last-input sequence, XYZ `int32` position and XYZ `int16` velocity, fixed-width values little-endian. A delta continues with unsigned LEB128 baseline tick and a one-byte mask: bit 0 last input, bits 1-3 XYZ position, bits 4-6 XYZ velocity. Changed last input is an unsigned difference; changed numeric axes are ZigZag/unsigned LEB128 differences, ordered position X, velocity X, position Y, velocity Y, position Z, velocity Z. Mask bit 7, overflowed/noncanonical integers, out-of-range state and trailing bytes are rejected. Payload size is bounded before parsing.

The moving case is about 68% smaller than the full record **for these exact values**. It is not a measured network-bandwidth reduction. These sizes include the sample record header but exclude batch framing, epoch, authentication, ACKs, spawn/despawn, transport/IP headers and retransmissions. Larger IDs/ticks/differences cost more. The codec emits an unchanged record when asked; the adapter can omit it after confirmation if its freshness policy permits.

The host exercises a synchronous local encode/decode loop and acknowledges it immediately. It is not a client connection, a latency model or a production replication scheduler. Its smoke mode also encodes full records as a size reference; production code need not perform that comparison twice.

## A baseline is received state, not sent state

The sender must retain bounded, immutable quantized snapshots and select a baseline actually acknowledged by that authenticated recipient. An ACK may only promote a record that this connection was sent in the current epoch. Never accept an arbitrary client tick as proof of receipt, and never change a snapshot's contents while retaining its identity/tick.

On receipt, `TryGetBaseline` provides a lookup hint for a delta's `(ActorId, Tick)`; it does not validate the whole record. Resolve it in the receiver's retained history, then call `TryRead`. Full records decode with `default` as the baseline. Keep old baselines long enough for packets encoded before a newer ACK arrived. Out-of-order packets can refer to older baselines even when the newest rendered snapshot is newer.

If a baseline is absent/evicted, do not apply the delta to the latest state or zero state. Request/rate-limit a full resynchronization; the sender can force a full record by passing `default`. Periodic keyframes and bounded history help recovery. Reject stale snapshots before updating presentation, and send ACKs only after a successful decode into retained history. The codec rejects a delta against the wrong ID/tick but does not manage ACK delivery, history, freshness or retries for you. See the original [snapshot compression discussion](https://gafferongames.com/post/snapshot_compression/) for the relationship between ACKs, packet loss and baseline selection.

Wrap these records in an authenticated/versioned session envelope with epoch and lifecycle ordering. The sample does not include encryption, a checksum, spawn generations, fragmentation, or an input-packet decoder. A record does not create an actor: resolve an already authorized spawn. Reset/replace history on reconnect, ID generation changes and origin changes. Do not replay a packet from a previous session into a current world, even if its numeric ID/tick matches.

## Estimate traffic, then measure it

A first payload estimate is:

```text
outbound bytes/second = clients * average relevant actors/client * updates/second * average bytes/record
```

Illustrative arithmetic, not an ABC capacity result: 1,000 clients observing 100 actors at 10 updates/second produce 24 MB/s (192 Mb/s) at 24 bytes/record, or 8 MB/s (64 Mb/s) at 8 bytes/record. That assumes every relevant actor updates each interval. It excludes transport overhead and other messages. Reducing relevant actors from 100 to 20 reduces this term another fivefold. Actual egress bills require your provider, region, transfer tier and operating hours; no hosting price is certified here.

CPU is also a budget: a 20 Hz simulation has 50 ms per step; 60 Hz has about 16.67 ms. That budget includes gameplay, physics, input validation, snapshot extraction and delivery work. Target headroom at high percentiles, not just an average that fits. Delta compression trades some CPU and per-peer history memory for fewer bytes; zero GC does not mean zero CPU, zero native allocation or zero retained memory.

## Production load-test gate

Test a built server and real or protocol-correct simulated clients using the selected SDK. Record hardware, OS, Unity/backend/build flags, scene contents, seeds, tick/send rates and network conditions. Increase these independently:

- concurrent connections and actors per zone;
- relevant actors per client, moving percentage and spawn/despawn rate;
- collision contacts, raycasts, AI/pathfinding work and persistence traffic;
- latency/jitter, loss, duplication, reordering, reconnects and slow recipients.

Measure p50/p95/p99 tick duration, missed ticks and backlog, whole-process CPU/native and managed memory, `GC.Alloc` and collections, bytes and packets per client, serializer time, history size, reliable queue depth, recovery time and game-correctness failures. Include burst joins and a soak test. Use separate load-driver capacity so the client generator is not mistaken for a server limit.

Only publish a player-per-instance limit after that workload stays correct within its budgets. Scale beyond an instance with explicit zone/session ownership and state handoff; `ActorNetworkMap` is not a distributed identity service. Release verification currently covers Windows x64 Mono, not Linux, IL2CPP or the optimized Dedicated Server target. See [networking checks](Networking.md#recorded-local-checks) and [performance methodology](Performance.md) for what has actually been measured.
