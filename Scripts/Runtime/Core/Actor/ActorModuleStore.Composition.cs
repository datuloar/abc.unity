using System;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace Abc.Unity
{
    internal sealed partial class ActorModuleStore
    {
        private static readonly ConditionalWeakTable<IActorModule, IActor> ModuleOwners = new ConditionalWeakTable<IActorModule, IActor>();

        private void AddBlueprintModules(ActorBlueprint blueprint)
        {
            var dataProviders = blueprint.Data;
            for (var i = 0; i < dataProviders.Count; i++)
            {
                var provider = dataProviders[i];
                if (provider == null)
                    throw new InvalidOperationException($"Blueprint {blueprint.name} contains a missing data provider at index {i}.");

                AddDataCore(provider.GetData());
            }

            var behaviourProviders = blueprint.Behaviours;
            for (var i = 0; i < behaviourProviders.Count; i++)
            {
                var provider = behaviourProviders[i];
                if (provider == null)
                    throw new InvalidOperationException($"Blueprint {blueprint.name} contains a missing behaviour provider at index {i}.");

                AddBehaviourCore(provider.GetBehaviour());
            }
        }

        private void AddDataCore(IActorData data)
        {
            if (data == null || data is UnityEngine.Object unityObject && unityObject == null)
                throw new ArgumentNullException(nameof(data));

            var dataType = data.GetType();
            if (dataType.IsValueType)
                throw new NotSupportedException($"Actor data {dataType.FullName} must be a reference type.");

            EnsureModuleOwnership(data);
            _data ??= new ActorModuleMap<IActorData>();

            if (!_data.TryAdd(data, out var existing))
            {
                if (ReferenceEquals(existing, data))
                    return;

                throw new InvalidOperationException($"Actor {_owner.Name} already contains data of type {dataType.FullName}.");
            }

            try
            {
                RegisterModule(data);
            }
            catch
            {
                _data.Remove(data);
                throw;
            }
        }

        private void AddBehaviourCore(IActorBehaviour behaviour)
        {
            if (behaviour == null || behaviour is UnityEngine.Object unityObject && unityObject == null)
                throw new ArgumentNullException(nameof(behaviour));

            var behaviourType = behaviour.GetType();
            if (behaviourType.IsValueType)
                throw new NotSupportedException($"Actor behaviour {behaviourType.FullName} must be a reference type.");

            EnsureModuleOwnership(behaviour);
            var owner = behaviour.Owner;
            if (owner != null && !(owner is UnityEngine.Object ownerObject && ownerObject == null) && !ReferenceEquals(owner, _owner))
                throw new InvalidOperationException($"Behaviour {behaviourType.FullName} already belongs to another actor.");

            _behaviours ??= new ActorModuleMap<IActorBehaviour>();

            if (!_behaviours.TryAdd(behaviour, out var existing))
            {
                if (ReferenceEquals(existing, behaviour))
                    return;

                throw new InvalidOperationException($"Actor {_owner.Name} already contains behaviour of type {behaviourType.FullName}.");
            }

            try
            {
                behaviour.Owner = _owner;

                if (ActorCommandRegistry.HandlesCommands(behaviourType))
                {
                    _commands ??= new ActorCommandRegistry(_owner);
                    _commands.Register(behaviour);
                }

                if (behaviour is IActorTick || behaviour is IActorFixedTick || behaviour is IActorLateTick)
                {
                    _ticks ??= new ActorTickRegistry(_owner);
                    _ticks.Add(behaviour);
                }

                RegisterModule(behaviour);
            }
            catch
            {
                UnregisterBehaviour(behaviour);
                if (_data?.ContainsReference(behaviour) != true)
                    ReleaseOwnershipSafely(behaviour);
                throw;
            }
        }

        private void RegisterModule(IActorModule module)
        {
            _modules ??= new ActorFastList<ModuleEntry>(1);

            for (var i = 0; i < _modules.Count; i++)
            {
                if (ReferenceEquals(_modules[i].Module, module))
                    return;
            }

            ModuleOwners.Add(module, _owner);

            try
            {
                _modules.Add(new ModuleEntry(module));
            }
            catch
            {
                ModuleOwners.Remove(module);
                throw;
            }
        }

        private void EnsureModuleOwnership(IActorModule module)
        {
            if (ModuleOwners.TryGetValue(module, out var owner) && !ReferenceEquals(owner, _owner))
                throw new InvalidOperationException($"Module {module.GetType().FullName} already belongs to another actor.");
        }

        private void UnregisterBehaviour(IActorBehaviour behaviour)
        {
            _behaviours?.Remove(behaviour);
            _commands?.Unregister(behaviour);
            _ticks?.Remove(behaviour);
        }

        private void ReleaseOwnershipSafely(IActorBehaviour behaviour)
        {
            try
            {
                if (ReferenceEquals(behaviour.Owner, _owner))
                    behaviour.Owner = null;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, behaviour as UnityEngine.Object ?? _owner as UnityEngine.Object);
            }
        }
    }
}
