using System;
using System.Runtime.CompilerServices;

namespace Abc.Unity
{
    internal sealed class DeferredList<T> where T : class
    {
        private T[] _items = Array.Empty<T>();
        private T[] _pending = Array.Empty<T>();
        private int _slotCount;
        private int _pendingCount;
        private int _iterationDepth;
        private int _liveCount;
        private bool _requiresCompaction;

        public int Count => _liveCount;

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _items[index];
        }

        public bool Contains(T item)
        {
            if (item == null)
                return false;

            for (var i = 0; i < _slotCount; i++)
            {
                if (ReferenceEquals(_items[i], item))
                    return true;
            }

            for (var i = 0; i < _pendingCount; i++)
            {
                if (ReferenceEquals(_pending[i], item))
                    return true;
            }

            return false;
        }

        public void Add(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (_iterationDepth == 0)
            {
                EnsureCapacity(ref _items, _slotCount + 1);
                _items[_slotCount++] = item;
            }
            else
            {
                EnsureCapacity(ref _pending, _pendingCount + 1);
                _pending[_pendingCount++] = item;
            }

            _liveCount++;
        }

        public bool Remove(T item)
        {
            if (item == null)
                return false;

            for (var i = 0; i < _pendingCount; i++)
            {
                if (!ReferenceEquals(_pending[i], item))
                    continue;

                RemovePendingAt(i);
                _liveCount--;
                return true;
            }

            for (var i = 0; i < _slotCount; i++)
            {
                if (!ReferenceEquals(_items[i], item))
                    continue;

                if (_iterationDepth == 0)
                    RemoveItemAt(i);
                else
                {
                    _items[i] = null;
                    _requiresCompaction = true;
                }

                _liveCount--;
                return true;
            }

            return false;
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
                throw new InvalidOperationException("Iteration was not started.");

            _iterationDepth--;

            if (_iterationDepth == 0)
                FlushDeferredChanges();
        }

        public void Clear()
        {
            if (_iterationDepth == 0)
            {
                Array.Clear(_items, 0, _slotCount);
                _slotCount = 0;
            }
            else
            {
                Array.Clear(_items, 0, _slotCount);
                _requiresCompaction = true;
            }

            Array.Clear(_pending, 0, _pendingCount);
            _pendingCount = 0;
            _liveCount = 0;
        }

        private void FlushDeferredChanges()
        {
            if (_requiresCompaction)
            {
                var writeIndex = 0;

                for (var readIndex = 0; readIndex < _slotCount; readIndex++)
                {
                    var item = _items[readIndex];
                    if (item != null)
                        _items[writeIndex++] = item;
                }

                Array.Clear(_items, writeIndex, _slotCount - writeIndex);
                _slotCount = writeIndex;
                _requiresCompaction = false;
            }

            if (_pendingCount == 0)
                return;

            EnsureCapacity(ref _items, _slotCount + _pendingCount);
            Array.Copy(_pending, 0, _items, _slotCount, _pendingCount);
            _slotCount += _pendingCount;
            Array.Clear(_pending, 0, _pendingCount);
            _pendingCount = 0;
        }

        private void RemoveItemAt(int index)
        {
            _slotCount--;

            if (index < _slotCount)
                Array.Copy(_items, index + 1, _items, index, _slotCount - index);

            _items[_slotCount] = null;
        }

        private void RemovePendingAt(int index)
        {
            _pendingCount--;

            if (index < _pendingCount)
                Array.Copy(_pending, index + 1, _pending, index, _pendingCount - index);

            _pending[_pendingCount] = null;
        }

        private static void EnsureCapacity(ref T[] array, int minimum)
        {
            if (array.Length >= minimum)
                return;

            var capacity = array.Length == 0 ? 1 : array.Length * 2;
            if (capacity < minimum)
                capacity = minimum;

            Array.Resize(ref array, capacity);
        }
    }
}
