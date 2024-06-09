#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using abc.unity.Core;

namespace abc.unity.CustomEditors
{
    [CustomEditor(typeof(ActorBlueprint))]
    [CanEditMultipleObjects]
    public class ActorBlueprintDrawer : Editor
    {
        private List<Type> _componentTypesCache;
        private List<Type> _behaviourTypesCache;
        private ReorderableList _dataList;
        private ReorderableList _behavioursList;
        private SerializedProperty _componentsProperty;
        private SerializedProperty _behavioursProperty;
        private SearchWindowProvider _searchComponentsProvider;
        private SearchWindowProvider _searchBehavioursProvider;

        private void OnEnable()
        {
            _componentTypesCache = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(ActorDataProviderBase)) && !t.IsAbstract).ToList();

            _behaviourTypesCache = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(ActorBehaviourProviderBase)) && !t.IsAbstract).ToList();

            _searchComponentsProvider = ScriptableObject.CreateInstance<SearchWindowProvider>();
            _searchComponentsProvider.Construct(_componentTypesCache);

            _searchBehavioursProvider = ScriptableObject.CreateInstance<SearchWindowProvider>();
            _searchBehavioursProvider.Construct(_behaviourTypesCache);

            _componentsProperty = serializedObject.FindProperty("_data");
            _behavioursProperty = serializedObject.FindProperty("_behaviours");
            _dataList = new ReorderableList(serializedObject, _componentsProperty, true, true, true, true);
            _behavioursList = new ReorderableList(serializedObject, _behavioursProperty, true, true, true, true);

            _dataList.drawHeaderCallback = (rect) => { EditorGUI.LabelField(rect, "_data"); };
            _dataList.drawElementCallback = DrawComponentElement;
            _dataList.elementHeightCallback = GetComponentElementHeight;
            _dataList.onAddCallback = AddComponent;
            _dataList.onRemoveCallback = RemoveElement;

            _behavioursList.drawHeaderCallback = (rect) => { EditorGUI.LabelField(rect, "_behaviours"); };
            _behavioursList.drawElementCallback = DrawBehaviourElement;
            _behavioursList.elementHeightCallback = GetBehaviourElementHeight;
            _behavioursList.onAddCallback = AddBehaviour;
            _behavioursList.onRemoveCallback = RemoveElement;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            _dataList.DoLayoutList();
            _behavioursList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProperties(SerializedProperty prop, bool drawChildren)
        {
            string lastPropPath = string.Empty;

            foreach (SerializedProperty p in prop)
            {
                if (p.isArray && p.propertyType == SerializedPropertyType.Generic)
                {
                    EditorGUILayout.BeginHorizontal();
                    p.isExpanded = EditorGUILayout.Foldout(p.isExpanded, p.displayName);
                    EditorGUILayout.EndHorizontal();
                    if (p.isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        DrawProperties(p, drawChildren);
                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(lastPropPath) && p.propertyPath.Contains(lastPropPath))
                        continue;

                    lastPropPath = p.propertyPath;
                    EditorGUILayout.PropertyField(p, drawChildren);
                }
            }
        }

        private void DrawProperties(Rect rect, SerializedProperty prop, bool drawChildren)
        {
            string lastPropPath = string.Empty;
            foreach (SerializedProperty p in prop)
            {
                rect.y += EditorGUIUtility.singleLineHeight;
                if (p.isArray && p.propertyType == SerializedPropertyType.Generic)
                {
                    p.isExpanded = EditorGUI.Foldout(rect, p.isExpanded, p.displayName);
                    if (p.isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        DrawProperties(rect, p, drawChildren);
                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(lastPropPath) && p.propertyPath.Contains(lastPropPath))
                        continue;
                    lastPropPath = p.propertyPath;
                    EditorGUI.PropertyField(rect, p, drawChildren);
                }
            }
        }

        private void DrawComponentElement(Rect rect, int index, bool isActive, bool isFocused, SerializedProperty element)
        {
            rect.x += 6;
            rect.width -= 10;
            rect.height = EditorGUIUtility.singleLineHeight;

            if (element.objectReferenceValue == null)
                return;

            var provider = element.objectReferenceValue as ActorDataProviderBase;
            var label = "";
            if (provider)
                label = provider.GetDataType().Name;
            element.isExpanded = EditorGUI.Foldout(rect, element.isExpanded, label);

            if (element.isExpanded)
            {
                var nestedObject = new SerializedObject(element.objectReferenceValue);
                var val = nestedObject.FindProperty("_value");

                foreach (SerializedProperty p in val)
                {
                    rect.y += EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(rect, p);
                }
                nestedObject.ApplyModifiedProperties();
            }

            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }

        private void DrawBehaviourElement(Rect rect, int index, bool isActive, bool isFocused, SerializedProperty element)
        {
            rect.x += 6;
            rect.width -= 10;
            rect.height = EditorGUIUtility.singleLineHeight;

            if (element.objectReferenceValue == null)
                return;

            var provider = element.objectReferenceValue as ActorBehaviourProviderBase;
            var label = "";
            if (provider)
                label = provider.GetBehaviourType().Name;
            element.isExpanded = EditorGUI.Foldout(rect, element.isExpanded, label);

            if (element.isExpanded)
            {
                var nestedObject = new SerializedObject(element.objectReferenceValue);
                var val = nestedObject.FindProperty("_value");

                foreach (SerializedProperty p in val)
                {
                    rect.y += EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(rect, p);
                }
                nestedObject.ApplyModifiedProperties();
            }

            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }

        private void DrawComponentElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _componentsProperty.GetArrayElementAtIndex(index);
            DrawComponentElement(rect, index, isActive, isFocused, element);
        }

        private void DrawBehaviourElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _behavioursProperty.GetArrayElementAtIndex(index);
            DrawBehaviourElement(rect, index, isActive, isFocused, element);
        }

        private float GetElementHeight(int index, SerializedProperty element)
        {
            float additionalProps = 0;
            var baseProp = EditorGUI.GetPropertyHeight(element, true);

            if (element.isExpanded && element.objectReferenceValue != null)
            {
                var nestedObject = new SerializedObject(element.objectReferenceValue);
                var val = nestedObject.FindProperty("_value");

                foreach (SerializedProperty p in val)
                {
                    additionalProps += EditorGUIUtility.singleLineHeight;
                }
            }

            var spacingBetweenElements = EditorGUIUtility.singleLineHeight / 2;
            return baseProp + spacingBetweenElements + additionalProps;
        }

        private float GetComponentElementHeight(int index)
        {
            var element = _componentsProperty.GetArrayElementAtIndex(index);
            return GetElementHeight(index, element);
        }

        private float GetBehaviourElementHeight(int index)
        {
            var element = _behavioursProperty.GetArrayElementAtIndex(index);
            return GetElementHeight(index, element);
        }

        private void AddComponent(ReorderableList l)
        {
            var pos = Event.current.mousePosition;
            pos.x -= 120;

            _searchComponentsProvider.EntrySelected = (o) =>
            {
                var type = (Type)o;
                var blueprint = target as ActorBlueprint;
                var list = l.serializedProperty;
                var index = list.arraySize;
                if (blueprint != null && type != null)
                {
                    var instance = (ActorDataProviderBase)ScriptableObject.CreateInstance(type);
                    instance.name = type.Name;
                    list.InsertArrayElementAtIndex(index);
                    var el = list.GetArrayElementAtIndex(index);
                    AssetDatabase.AddObjectToAsset(instance, blueprint);
                    EditorUtility.SetDirty(blueprint);
                    EditorUtility.SetDirty(instance);
                    AssetDatabase.SaveAssets();
                    el.objectReferenceValue = instance;
                    el.serializedObject.ApplyModifiedProperties();
                }
            };

            if (_componentTypesCache.Count == 0)
            {
                Debug.LogError("You are missing Components Providers, create a components provider and put it in a namespace");
                return;
            }

            SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(pos)), _searchComponentsProvider);
        }

        private void AddBehaviour(ReorderableList l)
        {
            var pos = Event.current.mousePosition;
            pos.x -= 120;

            _searchBehavioursProvider.EntrySelected = (o) =>
            {
                var type = (Type)o;
                var blueprint = target as ActorBlueprint;
                var list = l.serializedProperty;
                var index = list.arraySize;
                if (blueprint != null && type != null)
                {
                    var instance = (ActorBehaviourProviderBase)ScriptableObject.CreateInstance(type);
                    instance.name = type.Name;
                    list.InsertArrayElementAtIndex(index);
                    var el = list.GetArrayElementAtIndex(index);
                    AssetDatabase.AddObjectToAsset(instance, blueprint);
                    EditorUtility.SetDirty(blueprint);
                    EditorUtility.SetDirty(instance);
                    AssetDatabase.SaveAssets();
                    el.objectReferenceValue = instance;
                    el.serializedObject.ApplyModifiedProperties();
                }
            };

            if (_behaviourTypesCache.Count == 0)
            {
                Debug.LogError("You are missing Behavior Providers, create a behavior provider and put it in a namespace");
                return;
            }

            SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(pos)), _searchBehavioursProvider);
        }

        private void RemoveElement(ReorderableList l)
        {
            var blueprint = target as ActorBlueprint;
            var element = l.serializedProperty.GetArrayElementAtIndex(l.index);
            var obj = element.objectReferenceValue;
            if (obj != null)
            {
                DestroyImmediate(obj, true);
                EditorUtility.SetDirty(blueprint);
                AssetDatabase.SaveAssets();
                element.objectReferenceValue = null;
            }

            ReorderableList.defaultBehaviours.DoRemoveButton(l);
        }

        private void OnDestroy()
        {
            DestroyImmediate(_searchComponentsProvider);
            DestroyImmediate(_searchBehavioursProvider);
        }
    }
}
#endif