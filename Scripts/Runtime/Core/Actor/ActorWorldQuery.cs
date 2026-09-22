using System;

namespace Abc.Unity
{
    public readonly struct ActorWorldQuery<TData> where TData : class, IActorData
    {
        private readonly ActorWorld _world;
        private readonly ActorWorldDataIndex<TData> _index;
        private readonly ActorQueryFilter _filter;

        internal ActorWorldQuery(ActorWorld world, ActorWorldDataIndex<TData> index, ActorQueryFilter filter = default)
        {
            _world = world;
            _index = index;
            _filter = filter;
        }

        public int CandidateCount => _index?.Count ?? 0;

        public ActorWorldQuery<TData> WithTag(ActorTag tag) =>
            new ActorWorldQuery<TData>(_world, _index, _filter.WithTag(tag));

        public ActorWorldQuery<TData> WithoutTag(ActorTag tag) =>
            new ActorWorldQuery<TData>(_world, _index, _filter.WithoutTag(tag));

        public ActorWorldQuery<TData> OnlyAlive() =>
            new ActorWorldQuery<TData>(_world, _index, _filter.OnlyAlive());

        public int For<TAction>(ref TAction action) where TAction : IActorQueryAction<TData>
        {
            EnsureCreated();
            return _world.ExecuteQuery(_index, _filter, ref action);
        }

        public int For(Action<ActorModel, TData> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var wrapper = new ActorQueryDelegateAction<TData>(action);
            return For(ref wrapper);
        }

        private void EnsureCreated()
        {
            if (_world == null || _index == null)
                throw new InvalidOperationException("The actor query is not initialized.");
        }
    }

    public readonly struct ActorWorldQuery<TData1, TData2>
        where TData1 : class, IActorData
        where TData2 : class, IActorData
    {
        private readonly ActorWorld _world;
        private readonly ActorWorldDataIndex<TData1> _first;
        private readonly ActorWorldDataIndex<TData2> _second;
        private readonly ActorQueryFilter _filter;

        internal ActorWorldQuery(
            ActorWorld world,
            ActorWorldDataIndex<TData1> first,
            ActorWorldDataIndex<TData2> second,
            ActorQueryFilter filter = default)
        {
            _world = world;
            _first = first;
            _second = second;
            _filter = filter;
        }

        public int CandidateCount => Math.Min(_first?.Count ?? 0, _second?.Count ?? 0);

        public ActorWorldQuery<TData1, TData2> WithTag(ActorTag tag) =>
            new ActorWorldQuery<TData1, TData2>(_world, _first, _second, _filter.WithTag(tag));

        public ActorWorldQuery<TData1, TData2> WithoutTag(ActorTag tag) =>
            new ActorWorldQuery<TData1, TData2>(_world, _first, _second, _filter.WithoutTag(tag));

        public ActorWorldQuery<TData1, TData2> OnlyAlive() =>
            new ActorWorldQuery<TData1, TData2>(_world, _first, _second, _filter.OnlyAlive());

        public int For<TAction>(ref TAction action) where TAction : IActorQueryAction<TData1, TData2>
        {
            EnsureCreated();
            return _world.ExecuteQuery(_first, _second, _filter, ref action);
        }

        public int For(Action<ActorModel, TData1, TData2> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var wrapper = new ActorQueryDelegateAction<TData1, TData2>(action);
            return For(ref wrapper);
        }

        private void EnsureCreated()
        {
            if (_world == null || _first == null || _second == null)
                throw new InvalidOperationException("The actor query is not initialized.");
        }
    }

    public readonly struct ActorWorldQuery<TData1, TData2, TData3>
        where TData1 : class, IActorData
        where TData2 : class, IActorData
        where TData3 : class, IActorData
    {
        private readonly ActorWorld _world;
        private readonly ActorWorldDataIndex<TData1> _first;
        private readonly ActorWorldDataIndex<TData2> _second;
        private readonly ActorWorldDataIndex<TData3> _third;
        private readonly ActorQueryFilter _filter;

        internal ActorWorldQuery(
            ActorWorld world,
            ActorWorldDataIndex<TData1> first,
            ActorWorldDataIndex<TData2> second,
            ActorWorldDataIndex<TData3> third,
            ActorQueryFilter filter = default)
        {
            _world = world;
            _first = first;
            _second = second;
            _third = third;
            _filter = filter;
        }

        public int CandidateCount => Math.Min(_first?.Count ?? 0, Math.Min(_second?.Count ?? 0, _third?.Count ?? 0));

        public ActorWorldQuery<TData1, TData2, TData3> WithTag(ActorTag tag) =>
            new ActorWorldQuery<TData1, TData2, TData3>(_world, _first, _second, _third, _filter.WithTag(tag));

        public ActorWorldQuery<TData1, TData2, TData3> WithoutTag(ActorTag tag) =>
            new ActorWorldQuery<TData1, TData2, TData3>(_world, _first, _second, _third, _filter.WithoutTag(tag));

        public ActorWorldQuery<TData1, TData2, TData3> OnlyAlive() =>
            new ActorWorldQuery<TData1, TData2, TData3>(_world, _first, _second, _third, _filter.OnlyAlive());

        public int For<TAction>(ref TAction action) where TAction : IActorQueryAction<TData1, TData2, TData3>
        {
            EnsureCreated();
            return _world.ExecuteQuery(_first, _second, _third, _filter, ref action);
        }

        public int For(Action<ActorModel, TData1, TData2, TData3> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var wrapper = new ActorQueryDelegateAction<TData1, TData2, TData3>(action);
            return For(ref wrapper);
        }

        private void EnsureCreated()
        {
            if (_world == null || _first == null || _second == null || _third == null)
                throw new InvalidOperationException("The actor query is not initialized.");
        }
    }

    internal readonly struct ActorQueryDelegateAction<TData> : IActorQueryAction<TData>
        where TData : class, IActorData
    {
        private readonly Action<ActorModel, TData> _action;

        public ActorQueryDelegateAction(Action<ActorModel, TData> action) => _action = action;

        public void Execute(ActorModel actor, TData data) => _action(actor, data);
    }

    internal readonly struct ActorQueryDelegateAction<TData1, TData2> : IActorQueryAction<TData1, TData2>
        where TData1 : class, IActorData
        where TData2 : class, IActorData
    {
        private readonly Action<ActorModel, TData1, TData2> _action;

        public ActorQueryDelegateAction(Action<ActorModel, TData1, TData2> action) => _action = action;

        public void Execute(ActorModel actor, TData1 data1, TData2 data2) => _action(actor, data1, data2);
    }

    internal readonly struct ActorQueryDelegateAction<TData1, TData2, TData3> : IActorQueryAction<TData1, TData2, TData3>
        where TData1 : class, IActorData
        where TData2 : class, IActorData
        where TData3 : class, IActorData
    {
        private readonly Action<ActorModel, TData1, TData2, TData3> _action;

        public ActorQueryDelegateAction(Action<ActorModel, TData1, TData2, TData3> action) => _action = action;

        public void Execute(ActorModel actor, TData1 data1, TData2 data2, TData3 data3) =>
            _action(actor, data1, data2, data3);
    }
}
