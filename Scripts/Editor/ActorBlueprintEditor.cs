using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Editor
{
    [CustomEditor(typeof(ActorBlueprint))]
    internal sealed class ActorBlueprintEditor : UnityEditor.Editor
    {
        private sealed class ProviderOption
        {
            public Type ProviderType;
            public Type ModuleType;
            public GUIContent Label;
        }

        private readonly Dictionary<int, SerializedObject> _providerObjects = new Dictionary<int, SerializedObject>();
        private readonly HashSet<Type> _validationTypes = new HashSet<Type>();
        private List<ProviderOption> _dataOptions;
        private List<ProviderOption> _behaviourOptions;
        private ReorderableList _dataList;
        private ReorderableList _behaviourList;
        private SerializedProperty _data;
        private SerializedProperty _behaviours;

        private void OnEnable()
        {
            _data = serializedObject.FindProperty("_data");
            _behaviours = serializedObject.FindProperty("_behaviours");
            _dataOptions = BuildDataOptions();
            _behaviourOptions = BuildBehaviourOptions();
            _dataList = CreateList(_data, true);
            _behaviourList = CreateList(_behaviours, false);
            Undo.undoRedoPerformed += HandleUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
            _providerObjects.Clear();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ActorEditorStyles.DrawHeader("Actor Blueprint", "Reusable data and behaviour composition", "d_ScriptableObject Icon");
            DrawSummary();
            DrawValidation();

            ActorEditorStyles.BeginCard();
            _dataList.DoLayoutList();
            ActorEditorStyles.EndCard();

            ActorEditorStyles.BeginCard();
            _behaviourList.DoLayoutList();
            ActorEditorStyles.EndCard();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSummary()
        {
            EditorGUILayout.BeginHorizontal();
            ActorEditorStyles.DrawMetric(_data.arraySize.ToString(), "Data");
            ActorEditorStyles.DrawMetric(_behaviours.arraySize.ToString(), "Behaviours");
            ActorEditorStyles.DrawMetric((_data.arraySize + _behaviours.arraySize).ToString(), "Total Modules");
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(3f);
        }

        private void DrawValidation()
        {
            var missingData = CountMissing(_data);
            var missingBehaviours = CountMissing(_behaviours);
            var duplicateData = CountDuplicateModuleTypes(_data, true);
            var duplicateBehaviours = CountDuplicateModuleTypes(_behaviours, false);

            if (missingData + missingBehaviours > 0)
                EditorGUILayout.HelpBox("The blueprint contains missing providers. Remove or replace them before entering Play Mode.", MessageType.Error);

            if (duplicateData + duplicateBehaviours > 0)
                EditorGUILayout.HelpBox("Each concrete data or behaviour type can appear only once in an actor.", MessageType.Error);

            if (_data.arraySize + _behaviours.arraySize == 0)
                EditorGUILayout.HelpBox("Use the + buttons to compose this blueprint.", MessageType.Info);
        }

        private ReorderableList CreateList(SerializedProperty property, bool isData)
        {
            var list = new ReorderableList(serializedObject, property, true, true, true, true);
            list.drawHeaderCallback = rect => DrawListHeader(rect, property, isData);
            list.drawElementCallback = (rect, index, active, focused) => DrawElement(rect, property, index, isData);
            list.elementHeightCallback = index => GetElementHeight(property, index);
            list.onAddCallback = targetList => ShowProviderMenu(targetList, isData);
            list.onRemoveCallback = RemoveProvider;
            return list;
        }

        private static void DrawListHeader(Rect rect, SerializedProperty property, bool isData)
        {
            var title = isData ? "Data" : "Behaviours";
            EditorGUI.LabelField(rect, $"{title}  {property.arraySize}", EditorStyles.boldLabel);
        }

        private void DrawElement(Rect rect, SerializedProperty collection, int index, bool isData)
        {
            var element = collection.GetArrayElementAtIndex(index);
            var provider = element.objectReferenceValue;
            var background = isData ? ActorEditorStyles.DataColor : ActorEditorStyles.BehaviourColor;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + 1f, rect.width, rect.height - 2f), background);

            var line = new Rect(rect.x + 6f, rect.y + 3f, rect.width - 12f, EditorGUIUtility.singleLineHeight);
            if (provider == null)
            {
                EditorGUI.LabelField(line, "Missing Provider", EditorStyles.boldLabel);
                return;
            }

            var moduleType = GetModuleType(provider, isData);
            var badgeWidth = 76f;
            var foldoutRect = new Rect(line.x, line.y, line.width - badgeWidth, line.height);
            var badgeRect = new Rect(line.xMax - badgeWidth, line.y, badgeWidth, line.height);
            element.isExpanded = EditorGUI.Foldout(foldoutRect, element.isExpanded, moduleType.Name, true, EditorStyles.foldoutHeader);
            EditorGUI.LabelField(badgeRect, isData ? "DATA" : "BEHAVIOUR", EditorStyles.miniBoldLabel);

            if (!element.isExpanded)
                return;

            var nested = GetSerializedObject(provider);
            nested.UpdateIfRequiredOrScript();
            var property = nested.GetIterator();
            var enterChildren = true;
            var y = line.yMax + EditorGUIUtility.standardVerticalSpacing + 3f;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyPath == "m_Script")
                    continue;

                var height = EditorGUI.GetPropertyHeight(property, true);
                var propertyRect = new Rect(line.x + 12f, y, line.width - 12f, height);
                EditorGUI.PropertyField(propertyRect, property, true);
                y += height + EditorGUIUtility.standardVerticalSpacing;
            }

            nested.ApplyModifiedProperties();
        }

        private float GetElementHeight(SerializedProperty collection, int index)
        {
            var element = collection.GetArrayElementAtIndex(index);
            var height = EditorGUIUtility.singleLineHeight + 8f;

            if (!element.isExpanded || element.objectReferenceValue == null)
                return height;

            var nested = GetSerializedObject(element.objectReferenceValue);
            nested.UpdateIfRequiredOrScript();
            var property = nested.GetIterator();
            var enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyPath == "m_Script")
                    continue;

                height += EditorGUI.GetPropertyHeight(property, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            return height + 5f;
        }

        private void ShowProviderMenu(ReorderableList list, bool isData)
        {
            var options = isData ? _dataOptions : _behaviourOptions;
            if (options.Count == 0)
            {
                EditorUtility.DisplayDialog("ABC", isData ? "No data providers were found." : "No behaviour providers were found.", "OK");
                return;
            }

            var menu = new GenericMenu();

            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (ContainsModuleType(list.serializedProperty, option.ModuleType, isData))
                    menu.AddDisabledItem(new GUIContent($"{option.Label.text}  (already added)"));
                else
                    menu.AddItem(option.Label, false, () => AddProvider(list, option));
            }

            menu.ShowAsContext();
        }

        private void AddProvider(ReorderableList list, ProviderOption option)
        {
            var blueprint = (ActorBlueprint)target;
            var assetPath = AssetDatabase.GetAssetPath(blueprint);
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog("ABC", "Save the blueprint as an asset before adding providers.", "OK");
                return;
            }

            serializedObject.Update();
            var provider = ScriptableObject.CreateInstance(option.ProviderType);
            provider.name = option.ModuleType.Name;
            provider.hideFlags = HideFlags.HideInHierarchy;
            Undo.RecordObject(blueprint, "Add actor provider");
            Undo.RegisterCreatedObjectUndo(provider, "Add actor provider");
            AssetDatabase.AddObjectToAsset(provider, blueprint);

            var property = list.serializedProperty;
            var index = property.arraySize;
            property.InsertArrayElementAtIndex(index);
            property.GetArrayElementAtIndex(index).objectReferenceValue = provider;
            list.index = index;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(blueprint);
            EditorUtility.SetDirty(provider);
            AssetDatabase.SaveAssets();
        }

        private void RemoveProvider(ReorderableList list)
        {
            if (list.index < 0 || list.index >= list.serializedProperty.arraySize)
                return;

            serializedObject.Update();
            var blueprint = (ActorBlueprint)target;
            var property = list.serializedProperty;
            var element = property.GetArrayElementAtIndex(list.index);
            var provider = element.objectReferenceValue;
            Undo.RecordObject(blueprint, "Remove actor provider");
            element.objectReferenceValue = null;
            property.DeleteArrayElementAtIndex(list.index);
            serializedObject.ApplyModifiedProperties();

            if (provider != null)
            {
                _providerObjects.Remove(provider.GetInstanceID());
                Undo.DestroyObjectImmediate(provider);
            }

            EditorUtility.SetDirty(blueprint);
            AssetDatabase.SaveAssets();
        }

        private SerializedObject GetSerializedObject(UnityEngine.Object provider)
        {
            var id = provider.GetInstanceID();
            if (_providerObjects.TryGetValue(id, out var result) && result.targetObject != null)
                return result;

            result = new SerializedObject(provider);
            _providerObjects[id] = result;
            return result;
        }

        private static List<ProviderOption> BuildDataOptions()
        {
            var result = new List<ProviderOption>();

            foreach (var type in TypeCache.GetTypesDerivedFrom<ActorDataProviderBase>())
            {
                if (type.IsAbstract || type.IsGenericTypeDefinition)
                    continue;

                var provider = (ActorDataProviderBase)ScriptableObject.CreateInstance(type);
                var moduleType = provider.GetDataType();
                UnityEngine.Object.DestroyImmediate(provider);
                result.Add(CreateOption(type, moduleType));
            }

            SortOptions(result);
            return result;
        }

        private static List<ProviderOption> BuildBehaviourOptions()
        {
            var result = new List<ProviderOption>();

            foreach (var type in TypeCache.GetTypesDerivedFrom<ActorBehaviourProviderBase>())
            {
                if (type.IsAbstract || type.IsGenericTypeDefinition)
                    continue;

                var provider = (ActorBehaviourProviderBase)ScriptableObject.CreateInstance(type);
                var moduleType = provider.GetBehaviourType();
                UnityEngine.Object.DestroyImmediate(provider);
                result.Add(CreateOption(type, moduleType));
            }

            SortOptions(result);
            return result;
        }

        private static ProviderOption CreateOption(Type providerType, Type moduleType)
        {
            var namespacePath = string.IsNullOrEmpty(moduleType.Namespace)
                ? string.Empty
                : moduleType.Namespace.Replace('.', '/') + "/";
            return new ProviderOption
            {
                ProviderType = providerType,
                ModuleType = moduleType,
                Label = new GUIContent(namespacePath + ObjectNames.NicifyVariableName(moduleType.Name))
            };
        }

        private static void SortOptions(List<ProviderOption> options) =>
            options.Sort(static (left, right) => string.Compare(left.Label.text, right.Label.text, StringComparison.Ordinal));

        private static bool ContainsModuleType(SerializedProperty collection, Type moduleType, bool isData)
        {
            for (var i = 0; i < collection.arraySize; i++)
            {
                var provider = collection.GetArrayElementAtIndex(i).objectReferenceValue;
                if (provider != null && GetModuleType(provider, isData) == moduleType)
                    return true;
            }

            return false;
        }

        private static int CountMissing(SerializedProperty collection)
        {
            var result = 0;

            for (var i = 0; i < collection.arraySize; i++)
            {
                if (collection.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    result++;
            }

            return result;
        }

        private int CountDuplicateModuleTypes(SerializedProperty collection, bool isData)
        {
            _validationTypes.Clear();
            var result = 0;

            for (var i = 0; i < collection.arraySize; i++)
            {
                var provider = collection.GetArrayElementAtIndex(i).objectReferenceValue;
                if (provider != null && !_validationTypes.Add(GetModuleType(provider, isData)))
                    result++;
            }

            return result;
        }

        private static Type GetModuleType(UnityEngine.Object provider, bool isData) => isData
            ? ((ActorDataProviderBase)provider).GetDataType()
            : ((ActorBehaviourProviderBase)provider).GetBehaviourType();

        private void HandleUndoRedo()
        {
            _providerObjects.Clear();
            serializedObject.Update();
            Repaint();
        }
    }
}
