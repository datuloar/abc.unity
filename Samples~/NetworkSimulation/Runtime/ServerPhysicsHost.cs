using System;

using UnityEngine;

namespace Abc.Unity.Samples.NetworkSimulation
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ABC Samples/Server Physics Host")]
    public sealed class ServerPhysicsHost : MonoBehaviour
    {
        private const int TickRate = 60;
        private const int SnapshotIntervalSteps = 3;
        private const int MaxStepsPerFrame = 8;
        private const ulong SmokeSteps = 300;
        private static readonly ActorNetworkId BodyId = new ActorNetworkId(1);

        private readonly ServerSnapshot[] _snapshots = new ServerSnapshot[1];
        private readonly byte[] _packet = new byte[ServerSnapshotCodec.MaxPacketBytes];
        private QuantizedServerSnapshot _loopbackBaseline;
        private PhysicsServerSession _session;
        private double _accumulator;
        private bool _smoke;
        private bool _changedFrameRate;
        private int _previousTargetFrameRate;
        private int _snapshotCount;
        private int _snapshotBytes;
        private int _fullSnapshotBytes;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartSmokeTest()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-abcServerSmoke") < 0)
                return;
            var host = FindFirstObjectByType<ServerPhysicsHost>();
            if (host == null)
                host = new GameObject("ABC Headless Physics").AddComponent<ServerPhysicsHost>();
            host._smoke = true;
        }

        private void Start()
        {
            _session = new PhysicsServerSession(1f / TickRate);
            _session.Spawn(BodyId, 1, new Vector3(0f, 4f, 0f));
            if (Application.isBatchMode)
            {
                _previousTargetFrameRate = Application.targetFrameRate;
                Application.targetFrameRate = TickRate;
                _changedFrameRate = true;
            }
            Debug.Log("ABC server physics started. Inspect Network Server in World Explorer.");
        }

        private void Update()
        {
            try
            {
                _accumulator += Time.unscaledDeltaTime;
                var deltaTime = _session.Simulation.DeltaTime;
                var steps = 0;
                while (_accumulator >= deltaTime && steps++ < MaxStepsPerFrame)
                {
                    var input = new ServerInput(BodyId, _session.Simulation.Tick + 1, new Vector2(0.5f, 0f));
                    if (!_session.TryApplyInput(1, in input))
                        throw new InvalidOperationException("The server rejected valid local input.");
                    _session.Simulation.Step();
                    _accumulator -= deltaTime;
                    if (_session.Simulation.Tick % SnapshotIntervalSteps == 0)
                        CaptureLoopbackSnapshot();
                    if (_smoke && _session.Simulation.Tick == SmokeSteps)
                    {
                        CompleteSmokeTest();
                        return;
                    }
                }
            }
            catch (Exception exception)
            {
                enabled = false;
                Debug.LogException(exception, this);
                if (_smoke)
                    Application.Quit(1);
            }
        }

        private void CaptureLoopbackSnapshot()
        {
            _session.CopySnapshots(_snapshots);
            if (!QuantizedServerSnapshot.TryCreate(in _snapshots[0], out var quantized))
                throw new InvalidOperationException("Snapshot exceeds the sample wire range.");
            if (_smoke)
            {
                ServerSnapshotCodec.TryWrite(in quantized, default, _packet, out var fullBytes);
                _fullSnapshotBytes += fullBytes;
            }
            if (!ServerSnapshotCodec.TryWrite(in quantized, in _loopbackBaseline, _packet, out var length) ||
                !ServerSnapshotCodec.TryRead(_packet.AsSpan(0, length), in _loopbackBaseline, out var decoded))
                throw new InvalidOperationException("Snapshot loopback failed.");
            if (decoded.ActorId != quantized.ActorId || decoded.Tick != quantized.Tick ||
                decoded.LastInput != quantized.LastInput || decoded.Position != quantized.Position ||
                decoded.Velocity != quantized.Velocity)
                throw new InvalidOperationException("Snapshot loopback changed quantized state.");
            _loopbackBaseline = decoded;
            _snapshotCount++;
            _snapshotBytes += length;
        }

        private void CompleteSmokeTest()
        {
            _session.CopySnapshots(_snapshots);
            var snapshot = _snapshots[0];
            if (snapshot.Tick != SmokeSteps || snapshot.LastInput != SmokeSteps ||
                snapshot.Position.y < 0.45f || snapshot.Position.y > 0.6f || snapshot.Position.x < 5f)
                throw new InvalidOperationException("Server physics or snapshot validation failed.");
            if (_session.TryApplyInput(2, new ServerInput(BodyId, SmokeSteps + 1, Vector2.zero)))
                throw new InvalidOperationException("Unauthorized input was accepted.");
            if (_snapshotCount != (int)(SmokeSteps / SnapshotIntervalSteps) || _snapshotBytes >= _fullSnapshotBytes)
                throw new InvalidOperationException("Snapshot pacing or compression validation failed.");
            Debug.Log($"ABC_SNAPSHOT_CODEC_PASSED records={_snapshotCount} bytes={_snapshotBytes} fullBytes={_fullSnapshotBytes} localLoopback=true");
            Debug.Log($"ABC_SERVER_SMOKE_PASSED ticks={snapshot.Tick} position={snapshot.Position} lastInput={snapshot.LastInput}");
            enabled = false;
            _session.Dispose();
            Application.Quit(0);
        }

        private void OnDestroy()
        {
            _session?.Dispose();
            if (_changedFrameRate)
                Application.targetFrameRate = _previousTargetFrameRate;
        }
    }
}
