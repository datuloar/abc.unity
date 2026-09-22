using System;
using System.Runtime.CompilerServices;

namespace Abc.Unity
{
    public sealed partial class Actor
    {
        public void AddBlueprint(ActorBlueprint blueprint)
        {
            EnsureMutable();

            try
            {
                Modules.AddBlueprint(blueprint, _phase == LifecyclePhase.Initialized);
            }
            finally
            {
                RefreshUpdateRegistration();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasData<TData>() where TData : class, IActorData => _moduleStore?.HasData<TData>() == true;

        public void AddData<TData>(TData data) where TData : class, IActorData
        {
            EnsureMutable();
            Modules.AddData(data, _phase == LifecyclePhase.Initialized);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TData GetData<TData>() where TData : class, IActorData =>
            _moduleStore != null
                ? _moduleStore.GetData<TData>()
                : throw new InvalidOperationException($"Actor {name} does not contain data assignable to {typeof(TData).FullName}.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetData<TData>(out TData data) where TData : class, IActorData
        {
            if (_moduleStore != null)
                return _moduleStore.TryGetData(out data);

            data = null;
            return false;
        }

        public void RemoveData<TData>() where TData : class, IActorData
        {
            EnsureMutable();
            _moduleStore?.RemoveData<TData>();
        }

        public void AddBehaviour<TBehaviour>(TBehaviour behaviour) where TBehaviour : class, IActorBehaviour
        {
            EnsureMutable();

            try
            {
                Modules.AddBehaviour(behaviour, _phase == LifecyclePhase.Initialized);
            }
            finally
            {
                RefreshUpdateRegistration();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasBehaviour<TBehaviour>() where TBehaviour : class, IActorBehaviour =>
            _moduleStore?.HasBehaviour<TBehaviour>() == true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TBehaviour GetBehaviour<TBehaviour>() where TBehaviour : class, IActorBehaviour =>
            _moduleStore != null
                ? _moduleStore.GetBehaviour<TBehaviour>()
                : throw new InvalidOperationException($"Actor {name} does not contain behaviour assignable to {typeof(TBehaviour).FullName}.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetBehaviour<TBehaviour>(out TBehaviour behaviour) where TBehaviour : class, IActorBehaviour
        {
            if (_moduleStore != null)
                return _moduleStore.TryGetBehaviour(out behaviour);

            behaviour = null;
            return false;
        }

        public void RemoveBehaviour<TBehaviour>() where TBehaviour : class, IActorBehaviour
        {
            EnsureMutable();

            try
            {
                _moduleStore?.RemoveBehaviour<TBehaviour>();
            }
            finally
            {
                RefreshUpdateRegistration();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendCommand<TCommand>(TCommand command = default) where TCommand : IActorCommand =>
            _moduleStore?.SendCommand(command);
    }
}
