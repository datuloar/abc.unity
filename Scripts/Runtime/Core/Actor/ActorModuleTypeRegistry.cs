using System;
using System.Collections.Generic;

namespace Abc.Unity
{
    internal static class ActorModuleTypeRegistry<TModule> where TModule : class, IActorModule
    {
        private static readonly Dictionary<Type, int> IndexByType = new Dictionary<Type, int>();
        private static readonly object Sync = new object();

        public static int GetOrCreate(Type type)
        {
            lock (Sync)
            {
                if (IndexByType.TryGetValue(type, out var index))
                    return index;

                index = IndexByType.Count;
                IndexByType.Add(type, index);
                return index;
            }
        }
    }

    internal static class ActorModuleType<TModule, TConcrete>
        where TModule : class, IActorModule
        where TConcrete : class, TModule
    {
        public static readonly int Index = ActorModuleTypeRegistry<TModule>.GetOrCreate(typeof(TConcrete));
    }
}
