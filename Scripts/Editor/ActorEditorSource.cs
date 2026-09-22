using System;

using UnityEditor;

namespace Abc.Unity.Editor
{
    internal static class ActorEditorSource
    {
        internal static bool Open(Type type)
        {
            var candidates = AssetDatabase.FindAssets(type.Name + " t:MonoScript");
            for (var i = 0; i < candidates.Length; i++)
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(candidates[i]));
                if (script != null && script.GetClass() == type)
                    return AssetDatabase.OpenAsset(script);
            }

            return false;
        }
    }
}
