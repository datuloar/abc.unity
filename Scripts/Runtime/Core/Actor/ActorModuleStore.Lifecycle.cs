using System;

using UnityEngine;

namespace Abc.Unity
{
    internal sealed partial class ActorModuleStore
    {
        public void RollbackFrom(int checkpoint)
        {
            if (_modules == null)
                return;

            var previousLock = _mutationLocked;
            _mutationLocked = true;

            try
            {
                for (var i = _modules.Count - 1; _modules != null && i >= checkpoint; i--)
                    RemoveEntryAt(i, true);
            }
            finally
            {
                _mutationLocked = previousLock;

                if (_modules == null || _modules.Count == 0)
                    ReleaseStorage();
            }
        }

        public void CleanUpAll()
        {
            _mutationLocked = true;

            try
            {
                if (_modules != null)
                {
                    for (var i = _modules.Count - 1; i >= 0; i--)
                        RemoveEntryAt(i, true);
                }
            }
            finally
            {
                _ticks?.Clear();
                _commands?.Clear();
                _data?.Clear();
                _behaviours?.Clear();
                _modules?.Clear();
                _initializationDepth = 0;
                _mutationLocked = false;
                ReleaseStorage();
            }
        }

        private void InitializeFrom(int checkpoint)
        {
            if (_modules == null || checkpoint >= _modules.Count)
                return;

            var preInitializeIndex = checkpoint;
            var initializeIndex = checkpoint;
            _initializationDepth++;

            try
            {
                while (_modules != null && initializeIndex < _modules.Count)
                {
                    while (_modules != null && preInitializeIndex < _modules.Count)
                        PreInitializeAt(preInitializeIndex++);

                    if (_modules == null || initializeIndex >= _modules.Count)
                        break;

                    InitializeAt(initializeIndex++);
                }
            }
            finally
            {
                if (_initializationDepth > 0)
                    _initializationDepth--;
            }
        }

        private void PreInitializeAt(int index)
        {
            var module = _modules[index].Module;
            SetModuleState(index, ModuleState.PreInitializing);
            module.PreInitialize();

            if (IsModuleAt(index, module))
                SetModuleState(index, ModuleState.PreInitialized);
        }

        private void InitializeAt(int index)
        {
            var module = _modules[index].Module;
            SetModuleState(index, ModuleState.Initializing);
            module.Initialize();

            if (IsModuleAt(index, module))
                SetModuleState(index, ModuleState.Initialized);
        }

        private void SetModuleState(int index, ModuleState state)
        {
            var entry = _modules[index];
            entry.State = state;
            _modules[index] = entry;
        }

        private bool IsModuleAt(int index, IActorModule module) =>
            _modules != null && index < _modules.Count && ReferenceEquals(_modules[index].Module, module);

        private void ReleaseModuleIfUnused(IActorModule module)
        {
            if (_data?.ContainsReference(module) == true || _behaviours?.ContainsReference(module) == true || _modules == null)
                return;

            for (var i = 0; i < _modules.Count; i++)
            {
                if (!ReferenceEquals(_modules[i].Module, module))
                    continue;

                var entry = _modules[i];
                _modules.RemoveAt(i);
                CleanUpEntry(entry, false);
                return;
            }
        }

        private void RemoveEntryAt(int index, bool suppressErrors)
        {
            var entry = _modules[index];
            var module = entry.Module;

            if (module is IActorBehaviour behaviour)
                UnregisterBehaviour(behaviour);

            if (module is IActorData data)
                _data?.Remove(data);

            _modules.RemoveAt(index);
            CleanUpEntry(entry, suppressErrors);
        }

        private void CleanUpEntry(ModuleEntry entry, bool suppressErrors)
        {
            try
            {
                if (entry.State != ModuleState.Registered)
                    entry.Module.CleanUp();
            }
            catch (Exception exception) when (suppressErrors)
            {
                Debug.LogException(exception, entry.Module as UnityEngine.Object ?? _owner as UnityEngine.Object);
            }
            finally
            {
                if (entry.Module is IActorBehaviour behaviour)
                    ReleaseOwnershipSafely(behaviour);

                ModuleOwners.Remove(entry.Module);
            }
        }
    }
}
