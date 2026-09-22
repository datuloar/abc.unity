using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Abc.Unity
{
    internal sealed partial class ActorModuleStore
    {
        private enum ModuleState : byte
        {
            Registered,
            PreInitializing,
            PreInitialized,
            Initializing,
            Initialized
        }

        private struct ModuleEntry
        {
            public ModuleEntry(IActorModule module)
            {
                Module = module;
                State = ModuleState.Registered;
            }

            public IActorModule Module;
            public ModuleState State;
        }

        private readonly IActor _owner;
        private ActorFastList<ModuleEntry> _modules;
        private ActorModuleMap<IActorData> _data;
        private ActorModuleMap<IActorBehaviour> _behaviours;
        private ActorCommandRegistry _commands;
        private ActorTickRegistry _ticks;
        private int _initializationDepth;
        private bool _mutationLocked;

        public ActorModuleStore(IActor owner) => _owner = owner;

        public int Count => _modules?.Count ?? 0;
        public int DataCount => _data?.Count ?? 0;
        public int BehaviourCount => _behaviours?.Count ?? 0;
        public bool HasTickables => _ticks?.HasTickables == true;
        public bool HasFixedTickables => _ticks?.HasFixedTickables == true;
        public bool HasLateTickables => _ticks?.HasLateTickables == true;

#if UNITY_EDITOR
        internal void CopyModulesTo(List<IActorModule> destination)
        {
            if (_modules == null)
                return;

            for (var i = 0; i < _modules.Count; i++)
                destination.Add(_modules[i].Module);
        }
#endif

        public void AddBlueprint(ActorBlueprint blueprint, bool initializeImmediately)
        {
            EnsureMutationAllowed();

            if (blueprint == null)
                throw new ArgumentNullException(nameof(blueprint));

            var checkpoint = Count;

            try
            {
                AddBlueprintModules(blueprint);

                if (initializeImmediately && _initializationDepth == 0)
                    InitializeFrom(checkpoint);
            }
            catch
            {
                RollbackFrom(checkpoint);
                throw;
            }
        }

        public void AddData(IActorData data, bool initializeImmediately)
        {
            EnsureMutationAllowed();
            var checkpoint = Count;

            try
            {
                AddDataCore(data);

                if (initializeImmediately && _initializationDepth == 0)
                    InitializeFrom(checkpoint);
            }
            catch
            {
                RollbackFrom(checkpoint);
                throw;
            }
        }

        public void AddBehaviour(IActorBehaviour behaviour, bool initializeImmediately)
        {
            EnsureMutationAllowed();
            var checkpoint = Count;

            try
            {
                AddBehaviourCore(behaviour);

                if (initializeImmediately && _initializationDepth == 0)
                    InitializeFrom(checkpoint);
            }
            catch
            {
                RollbackFrom(checkpoint);
                throw;
            }
        }

        public void InitializeAll() => InitializeFrom(0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasData<TData>() where TData : class, IActorData =>
            _data != null && _data.Resolve<TData>(out _) == ActorModuleResolution.Found;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TData GetData<TData>() where TData : class, IActorData
        {
            if (_data == null)
                return ThrowDataResolution<TData>(ActorModuleResolution.Missing);

            var resolution = _data.Resolve<TData>(out var data);
            return resolution == ActorModuleResolution.Found
                ? (TData)data
                : ThrowDataResolution<TData>(resolution);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetData<TData>(out TData data) where TData : class, IActorData
        {
            if (_data != null && _data.Resolve<TData>(out var result) == ActorModuleResolution.Found)
            {
                data = (TData)result;
                return true;
            }

            data = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetExactData<TData>(out TData data) where TData : class, IActorData
        {
            if (_data != null)
                return _data.TryGetExact(out data);

            data = null;
            return false;
        }

        public void RemoveData<TData>() where TData : class, IActorData
        {
            EnsureRemovalAllowed();
            if (_data == null)
                return;

            var resolution = _data.Resolve<TData>(out var data);

            if (resolution == ActorModuleResolution.Missing)
                return;

            if (resolution == ActorModuleResolution.Ambiguous)
                throw CreateResolutionException(typeof(TData), resolution, "data");

            _data.Remove(data);
            ReleaseModuleIfUnused(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasBehaviour<TBehaviour>() where TBehaviour : class, IActorBehaviour =>
            _behaviours != null && _behaviours.Resolve<TBehaviour>(out _) == ActorModuleResolution.Found;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TBehaviour GetBehaviour<TBehaviour>() where TBehaviour : class, IActorBehaviour
        {
            if (_behaviours == null)
                return ThrowBehaviourResolution<TBehaviour>(ActorModuleResolution.Missing);

            var resolution = _behaviours.Resolve<TBehaviour>(out var behaviour);
            return resolution == ActorModuleResolution.Found
                ? (TBehaviour)behaviour
                : ThrowBehaviourResolution<TBehaviour>(resolution);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetBehaviour<TBehaviour>(out TBehaviour behaviour) where TBehaviour : class, IActorBehaviour
        {
            if (_behaviours != null && _behaviours.Resolve<TBehaviour>(out var result) == ActorModuleResolution.Found)
            {
                behaviour = (TBehaviour)result;
                return true;
            }

            behaviour = null;
            return false;
        }

        public void RemoveBehaviour<TBehaviour>() where TBehaviour : class, IActorBehaviour
        {
            EnsureRemovalAllowed();
            if (_behaviours == null)
                return;

            var resolution = _behaviours.Resolve<TBehaviour>(out var behaviour);

            if (resolution == ActorModuleResolution.Missing)
                return;

            if (resolution == ActorModuleResolution.Ambiguous)
                throw CreateResolutionException(typeof(TBehaviour), resolution, "behaviour");

            UnregisterBehaviour(behaviour);
            ReleaseModuleIfUnused(behaviour);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendCommand<TCommand>(TCommand command) where TCommand : IActorCommand => _commands?.Send(command);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tick(float deltaTime) => _ticks?.Tick(deltaTime);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FixedTick(float fixedDeltaTime) => _ticks?.FixedTick(fixedDeltaTime);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LateTick(float deltaTime) => _ticks?.LateTick(deltaTime);

        private void EnsureMutationAllowed()
        {
            if (_mutationLocked)
                throw new InvalidOperationException("Actor modules cannot be changed during cleanup or rollback.");
        }

        private void EnsureRemovalAllowed()
        {
            EnsureMutationAllowed();

            if (_initializationDepth > 0)
                throw new InvalidOperationException("Actor modules cannot be removed while modules are initializing.");
        }

        private TData ThrowDataResolution<TData>(ActorModuleResolution resolution) where TData : class, IActorData =>
            throw CreateResolutionException(typeof(TData), resolution, "data");

        private TBehaviour ThrowBehaviourResolution<TBehaviour>(ActorModuleResolution resolution) where TBehaviour : class, IActorBehaviour =>
            throw CreateResolutionException(typeof(TBehaviour), resolution, "behaviour");

        private InvalidOperationException CreateResolutionException(Type requestedType, ActorModuleResolution resolution, string category)
        {
            var reason = resolution == ActorModuleResolution.Ambiguous
                ? $"contains multiple {category} modules assignable to"
                : $"does not contain {category} assignable to";

            return new InvalidOperationException($"Actor {_owner.Name} {reason} {requestedType.FullName}.");
        }

        private void ReleaseStorage()
        {
            _modules = null;
            _data = null;
            _behaviours = null;
            _commands = null;
            _ticks = null;
        }
    }
}
