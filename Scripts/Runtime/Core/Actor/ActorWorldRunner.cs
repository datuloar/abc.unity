using System;

using UnityEngine;

namespace Abc.Unity
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ABC/Actor World Runner")]
    [HelpURL("https://github.com/datuloar/abc.unity#scale-without-a-rewrite")]
    [DefaultExecutionOrder(-9500)]
    public sealed class ActorWorldRunner : MonoBehaviour
    {
        [SerializeField] private string _worldName = "Gameplay";
        [SerializeField, Min(0)] private int _initialCapacity = 1024;
        [SerializeField] private bool _runUpdate = true;
        [SerializeField] private bool _runFixedUpdate = true;
        [SerializeField] private bool _runLateUpdate = true;

        private ActorWorld _world;

        public bool HasWorld => _world != null && !_world.IsDisposed;

        public ActorWorld World => HasWorld
            ? _world
            : throw new InvalidOperationException($"Actor world on {name} has not been created.");

        private void Awake() => GetOrCreateWorld();

        private void Update()
        {
            if (_runUpdate && HasWorld)
                _world.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_runFixedUpdate && HasWorld)
                _world.FixedTick(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            if (_runLateUpdate && HasWorld)
                _world.LateTick(Time.deltaTime);
        }

        private void OnDestroy() => DisposeWorld();

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_worldName))
                _worldName = "Gameplay";

            if (_initialCapacity < 0)
                _initialCapacity = 0;
        }

        public ActorWorld GetOrCreateWorld()
        {
            if (HasWorld)
                return _world;

            _world = new ActorWorld(_worldName, _initialCapacity);
            return _world;
        }

        public void DisposeWorld()
        {
            _world?.Dispose();
            _world = null;
        }
    }
}
