using System;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Abc.Unity.Samples.NetworkSimulation
{
    public sealed class PhysicsServerSession : IDisposable
    {
        private const int InputHoldSteps = 6;
        private const ulong InputSequenceWindow = 1024;

        private struct SnapshotAction : IActorQueryAction<ServerBodyData>
        {
            public ServerSnapshot[] Destination;
            public ulong Tick;
            public int Count;

            public void Execute(ActorModel actor, ServerBodyData data)
            {
                Destination[Count++] = new ServerSnapshot(
                    data.Id, Tick, data.LastAppliedSequence, data.Body.position, data.Velocity);
            }
        }

        private readonly ActorWorld _world;
        private readonly ActorNetworkMap _identities;
        private readonly ActorWorldQuery<ServerBodyData> _bodies;
        private readonly Scene _scene;
        private readonly PhysicsScene _physics;
        private bool _disposed;

        public PhysicsServerSession(float fixedDeltaTime = 1f / 60f, int capacity = 128)
        {
            _world = new ActorWorld("Network Server", capacity);
            _identities = new ActorNetworkMap(capacity);
            try
            {
                Simulation = new ActorSimulation(_world, fixedDeltaTime, SimulatePhysics);
                _bodies = _world.Query<ServerBodyData>();
                _scene = SceneManager.CreateScene("ABC Server " + Guid.NewGuid().ToString("N"),
                    new CreateSceneParameters(LocalPhysicsMode.Physics3D));
                _physics = _scene.GetPhysicsScene();
                CreateFloor();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public ActorSimulation Simulation { get; }
        public int Count => _identities.Count;

        public void Spawn(ActorNetworkId id, ulong authenticatedPeerId, Vector3 position)
        {
            EnsureActive();
            if (!id.IsValid || _identities.TryGetActor(id, out _))
                throw new ArgumentException("Spawn requires a unique valid ID.", nameof(id));
            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
                throw new ArgumentOutOfRangeException(nameof(position));

            var gameObject = CreateObject("Server Body " + id);
            ActorModel model = null;
            try
            {
                gameObject.transform.position = position;
                gameObject.AddComponent<SphereCollider>().radius = 0.5f;
                var body = gameObject.AddComponent<Rigidbody>();
                body.constraints = RigidbodyConstraints.FreezeRotation;
                model = new ActorModel(gameObject.name)
                    .WithData(new ServerBodyData(id, authenticatedPeerId, body))
                    .WithBehaviour(new ServerMoveBehaviour());
                _world.Add(model);
                _identities.Bind(id, model);
            }
            catch
            {
                model?.Dispose();
                DestroyObject(gameObject);
                throw;
            }
        }

        public bool Despawn(ActorNetworkId id)
        {
            EnsureActive();
            if (!_identities.TryGetActor(id, out var actor))
                return false;
            actor.Destroy();
            return true;
        }

        public bool TryApplyInput(ulong authenticatedPeerId, in ServerInput input)
        {
            EnsureActive();
            if (!_identities.TryGetActor(input.ActorId, out var actor) || !actor.IsAlive.Value)
                return false;
            var data = actor.GetData<ServerBodyData>();
            if (data.PeerId != authenticatedPeerId || input.Sequence <= data.LastSequence)
                return false;
            if (input.Sequence - data.LastSequence > InputSequenceWindow)
                return false;
            if (data.LastSequence != 0 && data.LastReceivedTick == Simulation.Tick)
                return false;
            var movement = input.Movement;
            if (!IsFinite(movement.x) || !IsFinite(movement.y) || movement.sqrMagnitude > 1f)
                return false;

            data.LastSequence = input.Sequence;
            data.LastReceivedTick = Simulation.Tick;
            data.Movement = movement;
            data.RemainingInputSteps = InputHoldSteps;
            return true;
        }

        public int CopySnapshots(ServerSnapshot[] destination)
        {
            EnsureActive();
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (destination.Length < _bodies.CandidateCount)
                throw new ArgumentException("Reserve one snapshot slot per body.", nameof(destination));

            var action = new SnapshotAction { Destination = destination, Tick = Simulation.Tick };
            _bodies.For(ref action);
            return action.Count;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _identities.Dispose();
            _world.Dispose();
            if (_scene.IsValid() && _scene.isLoaded)
                SceneManager.UnloadSceneAsync(_scene);
        }

        private void SimulatePhysics(float deltaTime)
        {
            EnsureActive();
            if (!_physics.IsValid())
                throw new InvalidOperationException("The server physics scene is no longer available.");
            _physics.Simulate(deltaTime);
        }

        private void CreateFloor()
        {
            var floor = CreateObject("Server Floor");
            floor.transform.position = Vector3.down * 0.5f;
            floor.AddComponent<BoxCollider>().size = new Vector3(1000f, 1f, 1000f);
        }

        private GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(gameObject, _scene);
            return gameObject;
        }

        private void EnsureActive()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PhysicsServerSession));
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static void DestroyObject(GameObject gameObject)
        {
            if (gameObject == null)
                return;
            gameObject.SetActive(false);
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(gameObject);
            else
                UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }
}
