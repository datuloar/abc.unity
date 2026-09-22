using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace Abc.Unity.Editor
{
    internal static class ActorProviderCatalog
    {
        internal sealed class Option
        {
            internal Type ProviderType { get; }
            internal Type ModuleType { get; }

            internal Option(Type providerType, Type moduleType)
            {
                ProviderType = providerType;
                ModuleType = moduleType;
            }
        }

        private static List<Option> _data;
        private static List<Option> _behaviours;

        internal static IReadOnlyList<Option> GetOptions(bool data) => data
            ? _data ??= Build<ActorDataProviderBase>()
            : _behaviours ??= Build<ActorBehaviourProviderBase>();

        internal static Type GetModuleType(UnityEngine.Object provider) => provider is ActorDataProviderBase data
            ? data.GetDataType()
            : ((ActorBehaviourProviderBase)provider).GetBehaviourType();

        private static List<Option> Build<T>() where T : ScriptableObject
        {
            var options = new List<Option>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<T>())
            {
                if (type.IsAbstract || type.ContainsGenericParameters)
                    continue;
                var provider = ScriptableObject.CreateInstance(type);
                try
                {
                    var moduleType = GetModuleType(provider);
                    if (moduleType != null)
                        options.Add(new Option(type, moduleType));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(provider);
                }
            }
            options.Sort(static (left, right) => string.Compare(left.ModuleType.FullName, right.ModuleType.FullName, StringComparison.Ordinal));
            return options;
        }
    }
}
