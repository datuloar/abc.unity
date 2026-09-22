using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Abc.Unity.Editor
{
    [CustomEditor(typeof(ActorBlueprint))]
    internal sealed class ActorBlueprintEditor : UnityEditor.Editor
    {
        private readonly List<SerializedObject> _providerObjects = new List<SerializedObject>();
        private readonly HashSet<int> _expanded = new HashSet<int>();
        private readonly HashSet<Type> _validationTypes = new HashSet<Type>();
        private VisualElement _root;
        private VisualElement _composition;
        private HelpBox _validation;
        private bool _rebuildPending;

        private void OnEnable() => Undo.undoRedoPerformed += ScheduleRebuild;

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= ScheduleRebuild;
            _root?.Unbind();
            ReleaseProviders();
        }

        public override VisualElement CreateInspectorGUI()
        {
            _root = ActorEditorStyles.Root();
            _root.Add(ActorEditorStyles.Header("Blueprint", "Reusable composition. Configure once, instantiate anywhere."));
            var tools = ActorEditorStyles.Row("abc-toolbar");
            tools.Add(ActorEditorStyles.Button("Add data", () => OpenPicker(true), true));
            tools.Add(ActorEditorStyles.Button("Add behaviour", () => OpenPicker(false), true));
            tools.Add(ActorEditorStyles.Button("Create module source", ActorFeatureWizard.Open));
            _root.Add(tools);
            _validation = new HelpBox("", HelpBoxMessageType.Error);
            _root.Add(_validation);
            _composition = new VisualElement();
            _root.Add(_composition);
            Rebuild();
            return _root;
        }

        private void ScheduleRebuild()
        {
            if (_root == null || _rebuildPending)
                return;
            _rebuildPending = true;
            _root.schedule.Execute(Rebuild);
        }

        private void Rebuild()
        {
            _rebuildPending = false;
            if (target == null || _composition == null)
                return;
            _composition.Unbind();
            _composition.Clear();
            ReleaseProviders();
            serializedObject.Update();
            var errors = new List<string>();
            BuildCollection("_data", true, errors);
            BuildCollection("_behaviours", false, errors);
            _validation.text = string.Join("\n", errors);
            _validation.style.display = errors.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void BuildCollection(string path, bool data, List<string> errors)
        {
            var collection = serializedObject.FindProperty(path);
            var card = ActorEditorStyles.Card($"{(data ? "Data" : "Behaviours")}  /  {collection.arraySize}",
                data ? "State and configuration." : "Logic with cached dependencies.");
            _validationTypes.Clear();
            for (var i = 0; i < collection.arraySize; i++)
            {
                var provider = collection.GetArrayElementAtIndex(i).objectReferenceValue;
                if (provider == null)
                    errors.Add($"{(data ? "Data" : "Behaviour")} slot {i + 1}: assign or remove the missing provider.");
                else if (!_validationTypes.Add(ActorProviderCatalog.GetModuleType(provider)))
                    errors.Add($"{provider.name}: this module type is added more than once.");
                card.Add(CreateProviderRow(path, i, data, provider));
            }
            if (collection.arraySize == 0)
                card.Add(ActorEditorStyles.Text(data ? "Add data to define this actor's state." : "Add behaviours to bring that state to life.", "abc-muted"));
            card.Add(ActorEditorStyles.Button(data ? "+ Add data" : "+ Add behaviour", () => OpenPicker(data)));
            _composition.Add(card);
        }

        private VisualElement CreateProviderRow(string path, int index, bool data, UnityEngine.Object provider)
        {
            var row = new VisualElement();
            row.AddToClassList("abc-provider");
            row.AddToClassList(data ? "abc-module-data" : "abc-module-behaviour");
            var actions = ActorEditorStyles.Row();
            actions.Add(ActorEditorStyles.Text($"{index + 1:00}", "abc-pill"));
            var up = ActorEditorStyles.Button("↑", () => MoveProvider(path, index, -1));
            up.tooltip = "Move earlier in initialization order";
            up.SetEnabled(index > 0);
            var down = ActorEditorStyles.Button("↓", () => MoveProvider(path, index, 1));
            down.tooltip = "Move later in initialization order";
            down.SetEnabled(index + 1 < serializedObject.FindProperty(path).arraySize);
            actions.Add(up);
            actions.Add(down);
            actions.Add(ActorEditorStyles.Button("Remove", () => RemoveProvider(path, index)));
            if (provider != null)
                actions.Add(ActorEditorStyles.Button("Source", () => ActorEditorSource.Open(ActorProviderCatalog.GetModuleType(provider))));
            row.Add(actions);
            if (provider == null)
            {
                var missing = new ObjectField("Provider") { objectType = data ? typeof(ActorDataProviderBase) : typeof(ActorBehaviourProviderBase), allowSceneObjects = false };
                missing.RegisterValueChangedCallback(evt => AssignProvider(path, index, evt.newValue));
                row.Add(missing);
                return row;
            }
            var foldout = new Foldout
            {
                text = ActorProviderCatalog.GetModuleType(provider).Name,
                value = _expanded.Contains(provider.GetInstanceID()),
                tooltip = provider.GetType().FullName
            };
            foldout.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    _expanded.Add(provider.GetInstanceID());
                else
                    _expanded.Remove(provider.GetInstanceID());
            });
            AddProviderFields(foldout, provider);
            row.Add(foldout);
            return row;
        }

        private void AddProviderFields(VisualElement parent, UnityEngine.Object provider)
        {
            var serialized = new SerializedObject(provider);
            _providerObjects.Add(serialized);
            var inspector = new InspectorElement(serialized);
            inspector.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                var script = inspector.Q<PropertyField>("PropertyField:m_Script");
                if (script != null)
                    script.style.display = DisplayStyle.None;
            });
            parent.Add(inspector);
        }

        private void OpenPicker(bool data)
        {
            ActorProviderPicker.Open(data, type => ContainsModule(data ? "_data" : "_behaviours", type),
                option => AddProvider(data ? "_data" : "_behaviours", option));
        }

        private bool ContainsModule(string path, Type type)
        {
            if (target == null)
                return true;
            serializedObject.UpdateIfRequiredOrScript();
            var collection = serializedObject.FindProperty(path);
            for (var i = 0; i < collection.arraySize; i++)
            {
                var provider = collection.GetArrayElementAtIndex(i).objectReferenceValue;
                if (provider != null && ActorProviderCatalog.GetModuleType(provider) == type)
                    return true;
            }
            return false;
        }

        private void AddProvider(string path, ActorProviderCatalog.Option option)
        {
            if (target == null || ContainsModule(path, option.ModuleType))
                return;
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target)))
            {
                EditorUtility.DisplayDialog("ABC Blueprint", "Save the blueprint as an asset before adding providers.", "Close");
                return;
            }
            serializedObject.Update();
            var provider = CreateInstance(option.ProviderType);
            provider.name = option.ModuleType.Name;
            provider.hideFlags = HideFlags.HideInHierarchy;
            Undo.RecordObject(target, "Add actor provider");
            Undo.RegisterCreatedObjectUndo(provider, "Add actor provider");
            AssetDatabase.AddObjectToAsset(provider, target);
            var collection = serializedObject.FindProperty(path);
            var index = collection.arraySize++;
            collection.GetArrayElementAtIndex(index).objectReferenceValue = provider;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(provider);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            _expanded.Add(provider.GetInstanceID());
            ScheduleRebuild();
        }

        private void MoveProvider(string path, int index, int direction)
        {
            serializedObject.Update();
            var collection = serializedObject.FindProperty(path);
            var destination = index + direction;
            if (index < 0 || index >= collection.arraySize || destination < 0 || destination >= collection.arraySize)
                return;
            collection.MoveArrayElement(index, destination);
            serializedObject.ApplyModifiedProperties();
            ScheduleRebuild();
        }

        private void AssignProvider(string path, int index, UnityEngine.Object provider)
        {
            serializedObject.Update();
            var collection = serializedObject.FindProperty(path);
            if (index >= collection.arraySize)
                return;
            collection.GetArrayElementAtIndex(index).objectReferenceValue = provider;
            serializedObject.ApplyModifiedProperties();
            ScheduleRebuild();
        }

        private void RemoveProvider(string path, int index)
        {
            ActorBlueprintAuthoring.RemoveProvider(serializedObject, serializedObject.FindProperty(path), index);
            ScheduleRebuild();
        }

        private void ReleaseProviders()
        {
            for (var i = 0; i < _providerObjects.Count; i++)
                _providerObjects[i].Dispose();
            _providerObjects.Clear();
        }
    }
}
