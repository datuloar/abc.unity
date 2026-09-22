using UnityEditor;
using UnityEngine;

namespace Abc.Unity.Editor
{
    internal static class ActorBlueprintAuthoring
    {
        public static void RemoveProvider(SerializedObject blueprint, SerializedProperty collection, int index)
        {
            if (index < 0 || index >= collection.arraySize)
                return;

            blueprint.Update();
            var element = collection.GetArrayElementAtIndex(index);
            var provider = element.objectReferenceValue;
            Undo.RecordObject(blueprint.targetObject, "Remove actor provider");
            element.objectReferenceValue = null;
            collection.DeleteArrayElementAtIndex(index);
            blueprint.ApplyModifiedProperties();

            if (provider != null && AssetDatabase.IsSubAsset(provider) &&
                AssetDatabase.GetAssetPath(provider) == AssetDatabase.GetAssetPath(blueprint.targetObject) &&
                !ContainsProvider((ActorBlueprint)blueprint.targetObject, provider))
                Undo.DestroyObjectImmediate(provider);

            EditorUtility.SetDirty(blueprint.targetObject);
            AssetDatabase.SaveAssets();
        }

        private static bool ContainsProvider(ActorBlueprint blueprint, Object provider)
        {
            for (var i = 0; i < blueprint.Data.Count; i++)
            {
                if (blueprint.Data[i] == provider)
                    return true;
            }

            for (var i = 0; i < blueprint.Behaviours.Count; i++)
            {
                if (blueprint.Behaviours[i] == provider)
                    return true;
            }

            return false;
        }
    }
}
