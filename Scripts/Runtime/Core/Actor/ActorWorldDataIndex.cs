using System;
using System.Runtime.CompilerServices;

namespace Abc.Unity
{
    internal interface IActorWorldDataIndex
    {
        int Count { get; }
        void Refresh(ActorModel actor);
        void Remove(ActorModel actor, int worldIndex);
        void MoveWorldSlot(int from, int to);
        void Clear();
    }

    internal sealed class ActorWorldDataIndex<TData> : IActorWorldDataIndex where TData : class, IActorData
    {
        private ActorModel[] _actors = Array.Empty<ActorModel>();
        private TData[] _data = Array.Empty<TData>();
        private ActorModel[] _pendingActors = Array.Empty<ActorModel>();
        private TData[] _pendingData = Array.Empty<TData>();
        private int[] _positionsByWorldSlot = Array.Empty<int>();
        private int _slotCount;
        private int _pendingCount;
        private int _iterationDepth;
        private int _liveCount;
        private bool _requiresCompaction;

        public int Count => _liveCount;

        public void Refresh(ActorModel actor)
        {
            if (!actor.IsActiveInWorld)
                return;

            var worldIndex = actor.WorldIndex;
            EnsurePositionCapacity(worldIndex + 1);
            var position = _positionsByWorldSlot[worldIndex];
            var hasData = actor.TryGetExactData<TData>(out var data);

            if (position == 0)
            {
                if (hasData)
                    Add(actor, data, worldIndex);

                return;
            }

            if (!hasData)
            {
                RemoveAt(position, actor, worldIndex);
                return;
            }

            if (position > 0)
                _data[position - 1] = data;
            else
                _pendingData[-position - 1] = data;
        }

        public void Remove(ActorModel actor, int worldIndex)
        {
            if ((uint)worldIndex >= (uint)_positionsByWorldSlot.Length)
                return;

            var position = _positionsByWorldSlot[worldIndex];
            if (position != 0)
                RemoveAt(position, actor, worldIndex);
        }

        public void MoveWorldSlot(int from, int to)
        {
            if (from == to || (uint)from >= (uint)_positionsByWorldSlot.Length)
                return;

            EnsurePositionCapacity(to + 1);
            _positionsByWorldSlot[to] = _positionsByWorldSlot[from];
            _positionsByWorldSlot[from] = 0;
        }

        public void Clear()
        {
            Array.Clear(_actors, 0, _slotCount);
            Array.Clear(_data, 0, _slotCount);
            Array.Clear(_pendingActors, 0, _pendingCount);
            Array.Clear(_pendingData, 0, _pendingCount);
            Array.Clear(_positionsByWorldSlot, 0, _positionsByWorldSlot.Length);
            _slotCount = 0;
            _pendingCount = 0;
            _liveCount = 0;
            _requiresCompaction = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BeginIteration()
        {
            _iterationDepth++;
            return _slotCount;
        }

        public void EndIteration()
        {
            if (_iterationDepth == 0)
                throw new InvalidOperationException("Query iteration was not started.");

            _iterationDepth--;
            if (_iterationDepth == 0)
                FlushDeferredChanges();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ActorModel GetActor(int index) => _actors[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TData GetData(int index) => _data[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(ActorModel actor, out TData data)
        {
            var worldIndex = actor.WorldIndex;
            if ((uint)worldIndex < (uint)_positionsByWorldSlot.Length)
            {
                var position = _positionsByWorldSlot[worldIndex];
                if (position > 0 && ReferenceEquals(_actors[position - 1], actor))
                {
                    data = _data[position - 1];
                    return true;
                }
            }

            data = null;
            return false;
        }

        private void Add(ActorModel actor, TData data, int worldIndex)
        {
            _liveCount++;

            if (_iterationDepth == 0)
            {
                EnsureActiveCapacity(_slotCount + 1);
                _actors[_slotCount] = actor;
                _data[_slotCount] = data;
                _positionsByWorldSlot[worldIndex] = ++_slotCount;
                return;
            }

            EnsurePendingCapacity(_pendingCount + 1);
            _pendingActors[_pendingCount] = actor;
            _pendingData[_pendingCount] = data;
            _positionsByWorldSlot[worldIndex] = -(++_pendingCount);
        }

        private void RemoveAt(int position, ActorModel actor, int worldIndex)
        {
            if (position > 0)
            {
                var index = position - 1;
                if (!ReferenceEquals(_actors[index], actor))
                    throw new InvalidOperationException("Actor query index is corrupted.");

                if (_iterationDepth == 0)
                    RemoveActiveAtSwapBack(index);
                else
                {
                    _actors[index] = null;
                    _data[index] = null;
                    _requiresCompaction = true;
                }
            }
            else
            {
                var index = -position - 1;
                if (!ReferenceEquals(_pendingActors[index], actor))
                    throw new InvalidOperationException("Actor query pending index is corrupted.");

                RemovePendingAtSwapBack(index);
            }

            _positionsByWorldSlot[worldIndex] = 0;
            _liveCount--;
        }

        private void RemoveActiveAtSwapBack(int index)
        {
            var lastIndex = --_slotCount;
            var replacement = _actors[lastIndex];
            var replacementData = _data[lastIndex];
            _actors[lastIndex] = null;
            _data[lastIndex] = null;

            if (index == lastIndex)
                return;

            _actors[index] = replacement;
            _data[index] = replacementData;
            _positionsByWorldSlot[replacement.WorldIndex] = index + 1;
        }

        private void RemovePendingAtSwapBack(int index)
        {
            var lastIndex = --_pendingCount;
            var replacement = _pendingActors[lastIndex];
            var replacementData = _pendingData[lastIndex];
            _pendingActors[lastIndex] = null;
            _pendingData[lastIndex] = null;

            if (index == lastIndex)
                return;

            _pendingActors[index] = replacement;
            _pendingData[index] = replacementData;
            _positionsByWorldSlot[replacement.WorldIndex] = -(index + 1);
        }

        private void FlushDeferredChanges()
        {
            if (_requiresCompaction)
            {
                var writeIndex = 0;

                for (var readIndex = 0; readIndex < _slotCount; readIndex++)
                {
                    var actor = _actors[readIndex];
                    if (actor == null)
                        continue;

                    _actors[writeIndex] = actor;
                    _data[writeIndex] = _data[readIndex];
                    _positionsByWorldSlot[actor.WorldIndex] = writeIndex + 1;
                    writeIndex++;
                }

                Array.Clear(_actors, writeIndex, _slotCount - writeIndex);
                Array.Clear(_data, writeIndex, _slotCount - writeIndex);
                _slotCount = writeIndex;
                _requiresCompaction = false;
            }

            if (_pendingCount == 0)
                return;

            EnsureActiveCapacity(_slotCount + _pendingCount);

            for (var i = 0; i < _pendingCount; i++)
            {
                var actor = _pendingActors[i];
                _actors[_slotCount] = actor;
                _data[_slotCount] = _pendingData[i];
                _positionsByWorldSlot[actor.WorldIndex] = ++_slotCount;
                _pendingActors[i] = null;
                _pendingData[i] = null;
            }

            _pendingCount = 0;
        }

        private void EnsureActiveCapacity(int minimum)
        {
            EnsureCapacity(ref _actors, minimum);
            EnsureCapacity(ref _data, minimum);
        }

        private void EnsurePendingCapacity(int minimum)
        {
            EnsureCapacity(ref _pendingActors, minimum);
            EnsureCapacity(ref _pendingData, minimum);
        }

        private void EnsurePositionCapacity(int minimum)
        {
            if (_positionsByWorldSlot.Length >= minimum)
                return;

            var capacity = GetCapacity(_positionsByWorldSlot.Length, minimum);
            Array.Resize(ref _positionsByWorldSlot, capacity);
        }

        private static void EnsureCapacity<T>(ref T[] array, int minimum)
        {
            if (array.Length >= minimum)
                return;

            Array.Resize(ref array, GetCapacity(array.Length, minimum));
        }

        private static int GetCapacity(int current, int minimum)
        {
            var capacity = current == 0 ? 4 : current * 2;
            return capacity < minimum ? minimum : capacity;
        }
    }
}
