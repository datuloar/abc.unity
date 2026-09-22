using System.Runtime.CompilerServices;

namespace Abc.Unity
{
    internal readonly struct ActorQueryFilter
    {
        private const byte RequireTag = 1;
        private const byte ExcludeTag = 2;
        private const byte RequireAlive = 4;

        private readonly byte _flags;
        private readonly ActorTag _requiredTag;
        private readonly ActorTag _excludedTag;

        private ActorQueryFilter(byte flags, ActorTag requiredTag, ActorTag excludedTag)
        {
            _flags = flags;
            _requiredTag = requiredTag;
            _excludedTag = excludedTag;
        }

        public ActorQueryFilter WithTag(ActorTag tag) =>
            new ActorQueryFilter((byte)(_flags | RequireTag), tag, _excludedTag);

        public ActorQueryFilter WithoutTag(ActorTag tag) =>
            new ActorQueryFilter((byte)(_flags | ExcludeTag), _requiredTag, tag);

        public ActorQueryFilter OnlyAlive() =>
            new ActorQueryFilter((byte)(_flags | RequireAlive), _requiredTag, _excludedTag);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(ActorModel actor)
        {
            if ((_flags & RequireAlive) != 0 && !actor.IsAliveValue)
                return false;

            var tag = actor.TagValue;
            return ((_flags & RequireTag) == 0 || tag.Equals(_requiredTag)) &&
                   ((_flags & ExcludeTag) == 0 || !tag.Equals(_excludedTag));
        }
    }
}
