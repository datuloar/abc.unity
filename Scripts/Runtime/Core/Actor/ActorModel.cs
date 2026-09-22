using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace Abc.Unity
{
    public sealed class ActorModel : IActor, IDisposable
    {
        private enum LifecyclePhase
        {
            Uninitialized,
            Initializing,
            Initialized,
            CleaningUp,
            Disposed
        }

        private ActorReactProperty<bool> _isAlive;
        private ActorReactProperty<bool> _isInitialized;
        private ActorReactProperty<ActorTag> _tag;
        private ActorModuleStore _modules;
        private bool _isAliveValue;
        private bool _isInitializedValue;
        private ActorTag _tagValue;
        private LifecyclePhase _phase;

        public ActorModel(string name = "Actor", ActorTag tag = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Actor name cannot be empty.", nameof(name));

            Name = name;
            _tagValue = tag;
            WorldIndex = -1;
        }

        public string Name { get; }
        public IReadOnlyActorReactProperty<bool> IsAlive => _isAlive ??= new ActorReactProperty<bool>(_isAliveValue);
        public IReadOnlyActorReactProperty<bool> IsInitialized => _isInitialized ??= new ActorReactProperty<bool>(_isInitializedValue);
        public IReadOnlyActorReactProperty<ActorTag> Tag => _tag ??= new ActorReactProperty<ActorTag>(_tagValue);

        public event Action Destroyed;

        internal ActorWorld World { get; set; }
        internal int WorldIndex { get; set; }
        internal bool IsPendingInWorld { get; set; }
        internal bool IsActiveInWorld { get; set; }

        private ActorModuleStore Modules => _modules ??= new ActorModuleStore(this);

        public void Initialize()
        {
            if (_phase == LifecyclePhase.Initialized)
                return;

            if (_phase == LifecyclePhase.Disposed)
                throw new ObjectDisposedException(Name);

            if (_phase != LifecyclePhase.Uninitialized)
                throw new InvalidOperationException($"Actor {Name} is already being initialized.");

            _phase = LifecyclePhase.Initializing;

            try
            {
                _modules?.InitializeAll();

                if (_phase == LifecyclePhase.Disposed)
                    return;

                _phase = LifecyclePhase.Initialized;
                SetInitialized(true);

                if (_phase != LifecyclePhase.Initialized)
                    return;

                SetAlive(true);
            }
            catch (Exception exception)
            {
                if (_phase == LifecyclePhase.Disposed)
                {
                    Debug.LogException(exception);
                    return;
                }

                _modules?.RollbackFrom(0);
                _phase = LifecyclePhase.Uninitialized;
                SetAliveSafely(false);
                SetInitializedSafely(false);
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tick(float deltaTime)
        {
            if (_phase == LifecyclePhase.Initialized && _isAliveValue)
                _modules?.Tick(deltaTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FixedTick(float fixedDeltaTime)
        {
            if (_phase == LifecyclePhase.Initialized && _isAliveValue)
                _modules?.FixedTick(fixedDeltaTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LateTick(float deltaTime)
        {
            if (_phase == LifecyclePhase.Initialized && _isAliveValue)
                _modules?.LateTick(deltaTime);
        }

        public void AddBlueprint(ActorBlueprint blueprint)
        {
            EnsureMutable();

            try
            {
                Modules.AddBlueprint(blueprint, _phase == LifecyclePhase.Initialized);
            }
            finally
            {
                World?.RefreshDataIndexes(this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasData<TData>() where TData : class, IActorData => _modules?.HasData<TData>() == true;

        public void AddData<TData>(TData data) where TData : class, IActorData
        {
            EnsureMutable();

            try
            {
                Modules.AddData(data, _phase == LifecyclePhase.Initialized);
            }
            finally
            {
                World?.RefreshDataIndexes(this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TData GetData<TData>() where TData : class, IActorData =>
            _modules != null
                ? _modules.GetData<TData>()
                : throw new InvalidOperationException($"Actor {Name} does not contain data assignable to {typeof(TData).FullName}.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetData<TData>(out TData data) where TData : class, IActorData
        {
            if (_modules != null)
                return _modules.TryGetData(out data);

            data = null;
            return false;
        }

        public void RemoveData<TData>() where TData : class, IActorData
        {
            EnsureMutable();

            try
            {
                _modules?.RemoveData<TData>();
            }
            finally
            {
                World?.RefreshDataIndexes(this);
            }
        }

        public void AddBehaviour<TBehaviour>(TBehaviour behaviour) where TBehaviour : class, IActorBehaviour
        {
            EnsureMutable();
            Modules.AddBehaviour(behaviour, _phase == LifecyclePhase.Initialized);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasBehaviour<TBehaviour>() where TBehaviour : class, IActorBehaviour => _modules?.HasBehaviour<TBehaviour>() == true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TBehaviour GetBehaviour<TBehaviour>() where TBehaviour : class, IActorBehaviour =>
            _modules != null
                ? _modules.GetBehaviour<TBehaviour>()
                : throw new InvalidOperationException($"Actor {Name} does not contain behaviour assignable to {typeof(TBehaviour).FullName}.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetBehaviour<TBehaviour>(out TBehaviour behaviour) where TBehaviour : class, IActorBehaviour
        {
            if (_modules != null)
                return _modules.TryGetBehaviour(out behaviour);

            behaviour = null;
            return false;
        }

        public void RemoveBehaviour<TBehaviour>() where TBehaviour : class, IActorBehaviour
        {
            EnsureMutable();
            _modules?.RemoveBehaviour<TBehaviour>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendCommand<TCommand>(TCommand command = default) where TCommand : IActorCommand =>
            _modules?.SendCommand(command);

        public void SetTag(ActorTag tag)
        {
            EnsureMutable();
            _tagValue = tag;

            try
            {
                if (_tag != null)
                    _tag.Value = tag;
            }
            finally
            {
                ActorRegistry.RefreshTag(this);
            }
        }

        public void Kill()
        {
            if (_phase == LifecyclePhase.Initialized)
                SetAlive(false);
        }

        public void Revive()
        {
            if (_phase == LifecyclePhase.Initialized)
                SetAlive(true);
        }

        public void Destroy()
        {
            if (_phase == LifecyclePhase.Disposed || _phase == LifecyclePhase.CleaningUp)
                return;

            _phase = LifecyclePhase.CleaningUp;
            SetAliveSafely(false);
            World?.Remove(this);
            _modules?.CleanUpAll();
            _modules = null;
            SetInitializedSafely(false);
            _phase = LifecyclePhase.Disposed;
            InvokeDestroyed();
        }

        public void Dispose() => Destroy();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetExactData<TData>(out TData data) where TData : class, IActorData
        {
            if (_modules != null)
                return _modules.TryGetExactData(out data);

            data = null;
            return false;
        }

        internal bool IsAliveValue => _isAliveValue;
        internal ActorTag TagValue => _tagValue;

#if UNITY_EDITOR
        internal string LifecycleState => _phase.ToString();
        internal int DataCount => _modules?.DataCount ?? 0;
        internal int BehaviourCount => _modules?.BehaviourCount ?? 0;

        internal void CopyModulesTo(List<IActorModule> destination)
        {
            destination.Clear();
            _modules?.CopyModulesTo(destination);
        }
#endif

        private void EnsureMutable()
        {
            if (_phase == LifecyclePhase.CleaningUp || _phase == LifecyclePhase.Disposed)
                throw new ObjectDisposedException(Name);
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
                    Debug.LogException(exception);
                }
            }
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

        private void SetAlive(bool value)
        {
            _isAliveValue = value;

            if (_isAlive != null)
                _isAlive.Value = value;
        }

        private void SetInitialized(bool value)
        {
            _isInitializedValue = value;

            if (_isInitialized != null)
                _isInitialized.Value = value;
        }

        private void SetAliveSafely(bool value)
        {
            _isAliveValue = value;

            if (_isAlive != null)
                SetReactiveValueSafely(_isAlive, value);
        }

        private void SetInitializedSafely(bool value)
        {
            _isInitializedValue = value;

            if (_isInitialized != null)
                SetReactiveValueSafely(_isInitialized, value);
        }
    }
}
