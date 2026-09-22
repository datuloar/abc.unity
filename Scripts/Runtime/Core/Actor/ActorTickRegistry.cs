using System;

using UnityEngine;

namespace Abc.Unity
{
    internal sealed class ActorTickRegistry
    {
        private readonly IActor _owner;
        private DeferredList<IActorTick> _tickables;
        private DeferredList<IActorFixedTick> _fixedTickables;
        private DeferredList<IActorLateTick> _lateTickables;

        public ActorTickRegistry(IActor owner) => _owner = owner;

        public bool HasTickables => _tickables?.Count > 0;
        public bool HasFixedTickables => _fixedTickables?.Count > 0;
        public bool HasLateTickables => _lateTickables?.Count > 0;

        public void Add(IActorBehaviour behaviour)
        {
            if (behaviour is IActorTick tickable)
                (_tickables ??= new DeferredList<IActorTick>()).Add(tickable);

            if (behaviour is IActorFixedTick fixedTickable)
                (_fixedTickables ??= new DeferredList<IActorFixedTick>()).Add(fixedTickable);

            if (behaviour is IActorLateTick lateTickable)
                (_lateTickables ??= new DeferredList<IActorLateTick>()).Add(lateTickable);
        }

        public void Remove(IActorBehaviour behaviour)
        {
            if (behaviour is IActorTick tickable)
                _tickables?.Remove(tickable);

            if (behaviour is IActorFixedTick fixedTickable)
                _fixedTickables?.Remove(fixedTickable);

            if (behaviour is IActorLateTick lateTickable)
                _lateTickables?.Remove(lateTickable);
        }

        public void Tick(float deltaTime)
        {
            var tickables = _tickables;
            if (tickables == null)
                return;

            var count = tickables.BeginIteration();

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var tickable = tickables[i];
                    if (tickable == null)
                        continue;

                    try
                    {
                        tickable.Tick(deltaTime);
                    }
                    catch (Exception exception)
                    {
                        LogException(exception, tickable);
                    }
                }
            }
            finally
            {
                tickables.EndIteration();
            }
        }

        public void FixedTick(float fixedDeltaTime)
        {
            var tickables = _fixedTickables;
            if (tickables == null)
                return;

            var count = tickables.BeginIteration();

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var tickable = tickables[i];
                    if (tickable == null)
                        continue;

                    try
                    {
                        tickable.FixedTick(fixedDeltaTime);
                    }
                    catch (Exception exception)
                    {
                        LogException(exception, tickable);
                    }
                }
            }
            finally
            {
                tickables.EndIteration();
            }
        }

        public void LateTick(float deltaTime)
        {
            var tickables = _lateTickables;
            if (tickables == null)
                return;

            var count = tickables.BeginIteration();

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var tickable = tickables[i];
                    if (tickable == null)
                        continue;

                    try
                    {
                        tickable.LateTick(deltaTime);
                    }
                    catch (Exception exception)
                    {
                        LogException(exception, tickable);
                    }
                }
            }
            finally
            {
                tickables.EndIteration();
            }
        }

        public void Clear()
        {
            _tickables?.Clear();
            _fixedTickables?.Clear();
            _lateTickables?.Clear();
            _tickables = null;
            _fixedTickables = null;
            _lateTickables = null;
        }

        private void LogException(Exception exception, object tickable) =>
            Debug.LogException(exception, tickable as UnityEngine.Object ?? _owner as UnityEngine.Object);
    }
}
