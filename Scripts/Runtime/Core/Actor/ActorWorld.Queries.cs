using System;

namespace Abc.Unity
{
    public sealed partial class ActorWorld
    {
        private readonly struct DataIndexEntry
        {
            public DataIndexEntry(int typeIndex, IActorWorldDataIndex index)
            {
                TypeIndex = typeIndex;
                Index = index;
            }

            public int TypeIndex { get; }
            public IActorWorldDataIndex Index { get; }
        }

        private DataIndexEntry[] _dataIndexes = Array.Empty<DataIndexEntry>();
        private int _dataIndexCount;

        internal int IndexedDataTypeCount => _dataIndexCount;

        public ActorWorldQuery<TData> Query<TData>() where TData : class, IActorData =>
            new ActorWorldQuery<TData>(this, GetDataIndex<TData>());

        public ActorWorldQuery<TData1, TData2> Query<TData1, TData2>()
            where TData1 : class, IActorData
            where TData2 : class, IActorData =>
            new ActorWorldQuery<TData1, TData2>(this, GetDataIndex<TData1>(), GetDataIndex<TData2>());

        public ActorWorldQuery<TData1, TData2, TData3> Query<TData1, TData2, TData3>()
            where TData1 : class, IActorData
            where TData2 : class, IActorData
            where TData3 : class, IActorData =>
            new ActorWorldQuery<TData1, TData2, TData3>(
                this,
                GetDataIndex<TData1>(),
                GetDataIndex<TData2>(),
                GetDataIndex<TData3>());

        internal void RefreshDataIndexes(ActorModel actor)
        {
            for (var i = 0; i < _dataIndexCount; i++)
                _dataIndexes[i].Index.Refresh(actor);
        }

        internal int ExecuteQuery<TData, TAction>(
            ActorWorldDataIndex<TData> index,
            ActorQueryFilter filter,
            ref TAction action)
            where TData : class, IActorData
            where TAction : IActorQueryAction<TData>
        {
            BeginQueryIteration();
            var count = index.BeginIteration();
            var matched = 0;

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var actor = index.GetActor(i);
                    if (actor == null || !filter.Matches(actor))
                        continue;

                    action.Execute(actor, index.GetData(i));
                    matched++;
                }
            }
            finally
            {
                index.EndIteration();
                EndQueryIteration();
            }

            return matched;
        }

        internal int ExecuteQuery<TData1, TData2, TAction>(
            ActorWorldDataIndex<TData1> first,
            ActorWorldDataIndex<TData2> second,
            ActorQueryFilter filter,
            ref TAction action)
            where TData1 : class, IActorData
            where TData2 : class, IActorData
            where TAction : IActorQueryAction<TData1, TData2>
        {
            BeginQueryIteration();
            first.BeginIteration();
            second.BeginIteration();

            try
            {
                return first.Count <= second.Count
                    ? ExecuteFromFirst(first, second, filter, ref action)
                    : ExecuteFromSecond(first, second, filter, ref action);
            }
            finally
            {
                second.EndIteration();
                first.EndIteration();
                EndQueryIteration();
            }
        }

        internal int ExecuteQuery<TData1, TData2, TData3, TAction>(
            ActorWorldDataIndex<TData1> first,
            ActorWorldDataIndex<TData2> second,
            ActorWorldDataIndex<TData3> third,
            ActorQueryFilter filter,
            ref TAction action)
            where TData1 : class, IActorData
            where TData2 : class, IActorData
            where TData3 : class, IActorData
            where TAction : IActorQueryAction<TData1, TData2, TData3>
        {
            BeginQueryIteration();
            first.BeginIteration();
            second.BeginIteration();
            third.BeginIteration();

            try
            {
                if (first.Count <= second.Count && first.Count <= third.Count)
                    return ExecuteFromFirst(first, second, third, filter, ref action);

                return second.Count <= third.Count
                    ? ExecuteFromSecond(first, second, third, filter, ref action)
                    : ExecuteFromThird(first, second, third, filter, ref action);
            }
            finally
            {
                third.EndIteration();
                second.EndIteration();
                first.EndIteration();
                EndQueryIteration();
            }
        }

        private ActorWorldDataIndex<TData> GetDataIndex<TData>() where TData : class, IActorData
        {
            EnsureAvailable();
            var dataType = typeof(TData);

            if (dataType.IsInterface || dataType.IsAbstract)
                throw new NotSupportedException($"World queries require a concrete data type, but {dataType.FullName} is not concrete.");

            var typeIndex = ActorModuleType<IActorData, TData>.Index;

            for (var i = 0; i < _dataIndexCount; i++)
            {
                var entry = _dataIndexes[i];
                if (entry.TypeIndex == typeIndex)
                    return (ActorWorldDataIndex<TData>)entry.Index;
            }

            var created = new ActorWorldDataIndex<TData>();
            EnsureDataIndexCapacity(_dataIndexCount + 1);
            _dataIndexes[_dataIndexCount] = new DataIndexEntry(typeIndex, created);
            _dataIndexCount++;

            for (var i = 0; i < _slotCount; i++)
            {
                var actor = _actors[i];
                if (actor != null)
                    created.Refresh(actor);
            }

            return created;
        }

        private void BeginQueryIteration()
        {
            EnsureAvailable();
            _iterationDepth++;
        }

        private void EndQueryIteration() => EndIteration();

        private void RemoveFromDataIndexes(ActorModel actor, int worldIndex)
        {
            for (var i = 0; i < _dataIndexCount; i++)
                _dataIndexes[i].Index.Remove(actor, worldIndex);
        }

        private void MoveDataIndexSlot(int from, int to)
        {
            for (var i = 0; i < _dataIndexCount; i++)
                _dataIndexes[i].Index.MoveWorldSlot(from, to);
        }

        private void ClearDataIndexes()
        {
            for (var i = 0; i < _dataIndexCount; i++)
                _dataIndexes[i].Index.Clear();
        }

        private void EnsureDataIndexCapacity(int minimum)
        {
            if (_dataIndexes.Length >= minimum)
                return;

            var capacity = _dataIndexes.Length == 0 ? 4 : _dataIndexes.Length * 2;
            if (capacity < minimum)
                capacity = minimum;

            Array.Resize(ref _dataIndexes, capacity);
        }

        private static int ExecuteFromFirst<TData1, TData2, TAction>(
            ActorWorldDataIndex<TData1> first,
            ActorWorldDataIndex<TData2> second,
            ActorQueryFilter filter,
            ref TAction action)
            where TData1 : class, IActorData
            where TData2 : class, IActorData
            where TAction : IActorQueryAction<TData1, TData2>
        {
            var count = first.BeginIteration();
            var matched = 0;

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var actor = first.GetActor(i);
                    if (actor == null || !filter.Matches(actor) || !second.TryGet(actor, out var data2))
                        continue;

                    action.Execute(actor, first.GetData(i), data2);
                    matched++;
                }
            }
            finally
            {
                first.EndIteration();
            }

            return matched;
        }

        private static int ExecuteFromSecond<TData1, TData2, TAction>(
            ActorWorldDataIndex<TData1> first,
            ActorWorldDataIndex<TData2> second,
            ActorQueryFilter filter,
            ref TAction action)
            where TData1 : class, IActorData
            where TData2 : class, IActorData
            where TAction : IActorQueryAction<TData1, TData2>
        {
            var count = second.BeginIteration();
            var matched = 0;

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var actor = second.GetActor(i);
                    if (actor == null || !filter.Matches(actor) || !first.TryGet(actor, out var data1))
                        continue;

                    action.Execute(actor, data1, second.GetData(i));
                    matched++;
                }
            }
            finally
            {
                second.EndIteration();
            }

            return matched;
        }

        private static int ExecuteFromFirst<TData1, TData2, TData3, TAction>(
            ActorWorldDataIndex<TData1> first,
            ActorWorldDataIndex<TData2> second,
            ActorWorldDataIndex<TData3> third,
            ActorQueryFilter filter,
            ref TAction action)
            where TData1 : class, IActorData
            where TData2 : class, IActorData
            where TData3 : class, IActorData
            where TAction : IActorQueryAction<TData1, TData2, TData3>
        {
            var count = first.BeginIteration();
            var matched = 0;

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var actor = first.GetActor(i);
                    if (actor == null || !filter.Matches(actor) ||
                        !second.TryGet(actor, out var data2) || !third.TryGet(actor, out var data3))
                        continue;

                    action.Execute(actor, first.GetData(i), data2, data3);
                    matched++;
                }
            }
            finally
            {
                first.EndIteration();
            }

            return matched;
        }

        private static int ExecuteFromSecond<TData1, TData2, TData3, TAction>(
            ActorWorldDataIndex<TData1> first,
            ActorWorldDataIndex<TData2> second,
            ActorWorldDataIndex<TData3> third,
            ActorQueryFilter filter,
            ref TAction action)
            where TData1 : class, IActorData
            where TData2 : class, IActorData
            where TData3 : class, IActorData
            where TAction : IActorQueryAction<TData1, TData2, TData3>
        {
            var count = second.BeginIteration();
            var matched = 0;

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var actor = second.GetActor(i);
                    if (actor == null || !filter.Matches(actor) ||
                        !first.TryGet(actor, out var data1) || !third.TryGet(actor, out var data3))
                        continue;

                    action.Execute(actor, data1, second.GetData(i), data3);
                    matched++;
                }
            }
            finally
            {
                second.EndIteration();
            }

            return matched;
        }

        private static int ExecuteFromThird<TData1, TData2, TData3, TAction>(
            ActorWorldDataIndex<TData1> first,
            ActorWorldDataIndex<TData2> second,
            ActorWorldDataIndex<TData3> third,
            ActorQueryFilter filter,
            ref TAction action)
            where TData1 : class, IActorData
            where TData2 : class, IActorData
            where TData3 : class, IActorData
            where TAction : IActorQueryAction<TData1, TData2, TData3>
        {
            var count = third.BeginIteration();
            var matched = 0;

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var actor = third.GetActor(i);
                    if (actor == null || !filter.Matches(actor) ||
                        !first.TryGet(actor, out var data1) || !second.TryGet(actor, out var data2))
                        continue;

                    action.Execute(actor, data1, data2, third.GetData(i));
                    matched++;
                }
            }
            finally
            {
                third.EndIteration();
            }

            return matched;
        }
    }
}
