using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace Abc.Unity
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ABC/Actor")]
    [HelpURL("https://github.com/datuloar/abc.unity")]
    [DefaultExecutionOrder(-10000)]
    public sealed partial class Actor : MonoBehaviour, IActor, IActorView
    {
        private enum LifecyclePhase
        {
            Uninitialized,
            Initializing,
            Initialized,
            CleaningUp,
            Disposed
        }

        private readonly ActorReactProperty<bool> _isAlive = new ActorReactProperty<bool>();
        private readonly ActorReactProperty<bool> _isInitialized = new ActorReactProperty<bool>();
        private readonly ActorUpdateRegistration _updateRegistration = new ActorUpdateRegistration();
        private readonly List<MonoBehaviour> _componentBuffer = new List<MonoBehaviour>();

        [SerializeField] private ActorReactProperty<ActorTag> _tag = new ActorReactProperty<ActorTag>();
        [SerializeField] private List<ActorBlueprint> _blueprints = new List<ActorBlueprint>();
        [SerializeField] private bool _initializeOnAwake = true;
        [SerializeField] private bool _hasUpdate = true;
        [SerializeField] private bool _hasFixedUpdate = true;
        [SerializeField] private bool _hasLateUpdate = true;

        private ActorModuleStore _moduleStore;
        private LifecyclePhase _phase;
        private bool _registryRegistered;

        public string Name => name;
        public IReadOnlyActorReactProperty<bool> IsAlive => _isAlive;
        public IReadOnlyActorReactProperty<bool> IsInitialized => _isInitialized;
        public IReadOnlyActorReactProperty<ActorTag> Tag => _tag;

        public event Action Destroyed;

        internal string LifecycleState => _phase.ToString();
        internal int DataCount => _moduleStore?.DataCount ?? 0;
        internal int BehaviourCount => _moduleStore?.BehaviourCount ?? 0;

        private ActorModuleStore Modules => _moduleStore ??= new ActorModuleStore(this);

        private void Awake()
        {
            _tag ??= new ActorReactProperty<ActorTag>();
            _blueprints ??= new List<ActorBlueprint>();

            if (_initializeOnAwake)
                Initialize();
        }

        private void OnEnable()
        {
            if (_phase == LifecyclePhase.Initialized)
                RefreshUpdateRegistration();
        }

        private void OnDisable() => _updateRegistration.RemoveAll(this);

        private void OnDestroy() => ShutDown();

        public void Initialize()
        {
            if (_phase == LifecyclePhase.Initialized)
                return;

            if (_phase == LifecyclePhase.Disposed)
                throw new ObjectDisposedException(nameof(Actor));

            if (_phase != LifecyclePhase.Uninitialized)
                throw new InvalidOperationException($"Actor {name} is already being initialized.");

            _phase = LifecyclePhase.Initializing;

            try
            {
                ActorModuleCollector.Collect(this, _blueprints, _componentBuffer, Modules);
                Modules.InitializeAll();

                if (_phase == LifecyclePhase.Disposed)
                    return;

                _phase = LifecyclePhase.Initialized;
                _isInitialized.Value = true;

                if (_phase != LifecyclePhase.Initialized)
                    return;

                _isAlive.Value = true;

                if (_phase != LifecyclePhase.Initialized)
                    return;

                RegisterInRegistry();

                if (_phase == LifecyclePhase.Initialized)
                    RefreshUpdateRegistration();
            }
            catch (Exception exception)
            {
                if (_phase == LifecyclePhase.Disposed)
                {
                    Debug.LogException(exception, this);
                    return;
                }

                RollbackInitialization();
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tick(float deltaTime)
        {
            if (!CanTick())
                return;

            Modules.Tick(deltaTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FixedTick(float fixedDeltaTime)
        {
            if (!CanTick())
                return;

            Modules.FixedTick(fixedDeltaTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LateTick(float deltaTime)
        {
            if (!CanTick())
                return;

            Modules.LateTick(deltaTime);
        }

        public void SetTag(ActorTag tag)
        {
            EnsureMutable();

            try
            {
                _tag.Value = tag;
            }
            finally
            {
                ActorRegistry.RefreshTag(this);
            }
        }

        public void Kill()
        {
            if (_phase != LifecyclePhase.Initialized)
                return;

            try
            {
                _isAlive.Value = false;
            }
            finally
            {
                RefreshUpdateRegistration();
            }
        }

        public void Revive()
        {
            if (_phase != LifecyclePhase.Initialized)
                return;

            try
            {
                _isAlive.Value = true;
            }
            finally
            {
                RefreshUpdateRegistration();
            }
        }

        public void Destroy()
        {
            if (_phase == LifecyclePhase.Disposed)
                return;

            ShutDown();

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(gameObject);
            else
                UnityEngine.Object.DestroyImmediate(gameObject);
        }

        private bool CanTick() =>
            _phase == LifecyclePhase.Initialized && _isAlive.Value && isActiveAndEnabled;

        private void RegisterInRegistry()
        {
            _registryRegistered = true;

            try
            {
                ActorRegistry.Add(this);
            }
            catch
            {
                _registryRegistered = false;
                throw;
            }
        }

        private void RollbackInitialization()
        {
            _updateRegistration.RemoveAll(this);

            if (_registryRegistered)
            {
                ActorRegistry.Remove(this);
                _registryRegistered = false;
            }

            Modules.RollbackFrom(0);
            _phase = LifecyclePhase.Uninitialized;
            SetReactiveValueSafely(_isAlive, false);
            SetReactiveValueSafely(_isInitialized, false);
        }

        private void RefreshUpdateRegistration()
        {
            var canRegister = _phase == LifecyclePhase.Initialized && _isAlive.Value && isActiveAndEnabled;
            var modules = _moduleStore;
            var tick = canRegister && _hasUpdate && modules?.HasTickables == true;
            var fixedTick = canRegister && _hasFixedUpdate && modules?.HasFixedTickables == true;
            var lateTick = canRegister && _hasLateUpdate && modules?.HasLateTickables == true;
            _updateRegistration.Refresh(this, tick, fixedTick, lateTick);
        }

        private void ShutDown()
        {
            if (_phase == LifecyclePhase.Disposed || _phase == LifecyclePhase.CleaningUp)
                return;

            _phase = LifecyclePhase.CleaningUp;
            SetReactiveValueSafely(_isAlive, false);
            _updateRegistration.RemoveAll(this);

            if (_registryRegistered)
            {
                ActorRegistry.Remove(this);
                _registryRegistered = false;
            }

            _moduleStore?.CleanUpAll();
            SetReactiveValueSafely(_isInitialized, false);
            _phase = LifecyclePhase.Disposed;
            InvokeDestroyed();
        }

        private void InvokeDestroyed()
        {
            var handlers = Destroyed?.GetInvocationList();
            Destroyed = null;

            if (handlers == null)
                return;

            for (var i = 0; i < handlers.Length; i++)
            {
                try
                {
                    ((Action)handlers[i]).Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }

        private void EnsureMutable()
        {
            if (_phase == LifecyclePhase.CleaningUp || _phase == LifecyclePhase.Disposed)
                throw new ObjectDisposedException(nameof(Actor));
        }

        private static void SetReactiveValueSafely(ActorReactProperty<bool> property, bool value)
        {
            try
            {
                property.Value = value;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
