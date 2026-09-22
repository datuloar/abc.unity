using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Abc.Unity
{
    internal sealed class ActorFastList<T>
    {
        private const int MaxArrayLength = 0x7FEFFFFF;

        private T[] _items = Array.Empty<T>();

        public ActorFastList()
        {
        }

        public ActorFastList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            if (capacity > 0)
                _items = new T[capacity];
        }

        public int Count { get; private set; }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _items.Length;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value < Count)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (value != _items.Length)
                    Array.Resize(ref _items, value);
            }
        }

        public Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Span<T>(_items, 0, Count);
        }

        public ReadOnlySpan<T> ReadOnlySpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new ReadOnlySpan<T>(_items, 0, Count);
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return ref _items[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            EnsureCapacity(Count + 1);

            _items[Count++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T item)
        {
            var index = IndexOf(item);

            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            Count--;

            if (index < Count)
                Array.Copy(_items, index + 1, _items, index, Count - index);

            _items[Count] = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAtSwapBack(int index)
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var lastIndex = --Count;
            _items[index] = _items[lastIndex];
            _items[lastIndex] = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T item) => Array.IndexOf(_items, item, 0, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item) => IndexOf(item) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, T item)
        {
            if ((uint)index > (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            EnsureCapacity(Count + 1);

            if (index < Count)
                Array.Copy(_items, index, _items, index + 1, Count - index);

            _items[index] = item;
            Count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BinarySearch(T item) => BinarySearch(0, Count, item, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BinarySearch(T item, IComparer<T> comparer) => BinarySearch(0, Count, item, comparer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer) => Array.BinarySearch(_items, index, count, item, comparer ?? Comparer<T>.Default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (Count == 0)
                return;

            Array.Clear(_items, 0, Count);
            Count = 0;
        }

        public void TrimExcess()
        {
            if (Count == _items.Length)
                return;

            Array.Resize(ref _items, Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int RemoveAll(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            var freeIndex = 0;

            while (freeIndex < Count && !match(_items[freeIndex]))
                freeIndex++;

            if (freeIndex >= Count)
                return 0;

            var current = freeIndex + 1;

            while (current < Count)
            {
                while (current < Count && match(_items[current])) current++;

                if (current < Count)
                    _items[freeIndex++] = _items[current++];
            }

            Array.Clear(_items, freeIndex, Count - freeIndex);
            var result = Count - freeIndex;
            Count = freeIndex;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int min)
        {
            if (_items.Length >= min)
                return;

            if (min < 0 || min > MaxArrayLength)
                throw new OutOfMemoryException();

            var nextCapacity = _items.Length == 0 ? 4 : _items.Length * 2;

            if ((uint)nextCapacity > MaxArrayLength)
                nextCapacity = MaxArrayLength;

            if (nextCapacity < min)
                nextCapacity = min;

            Capacity = nextCapacity;
        }
    }
}
