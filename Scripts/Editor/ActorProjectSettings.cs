using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Abc.Unity.Editor
{
    [FilePath("ProjectSettings/ABC.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class ActorProjectSettings : ScriptableSingleton<ActorProjectSettings>
    {
        [SerializeField] private string _featureFolder = "Assets";
        [SerializeField] private string _featureNamespace = "Game.Features";
        [SerializeField, HideInInspector] private string _folderGuid;
        [SerializeField, HideInInspector] private bool _configured;

        internal string FeatureFolder => ResolveFolder(_featureFolder, _folderGuid);
        internal string FeatureNamespace => _featureNamespace;
        internal bool IsConfigured => _configured;

        private void OnEnable() => Undo.undoRedoPerformed += Persist;

        private void OnDisable() => Undo.undoRedoPerformed -= Persist;

        internal static string ResolveFolder(string fallback, string guid)
        {
            var current = string.IsNullOrEmpty(guid) ? string.Empty : AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.IsValidFolder(current) ? current : fallback;
        }

        internal void SaveDefaults(string folder, string namespaceName)
        {
            Undo.RecordObject(this, "Change ABC defaults");
            _featureFolder = folder.Replace('\\', '/').Trim().TrimEnd('/');
            _featureNamespace = namespaceName.Trim();
            _folderGuid = AssetDatabase.IsValidFolder(_featureFolder)
                ? AssetDatabase.AssetPathToGUID(_featureFolder)
                : string.Empty;
            _configured = true;
            Save(true);
        }

        internal void Persist() => Save(true);

        [MenuItem("Tools/ABC/Project Setup", priority = 1203)]
        internal static void Open() => SettingsService.OpenProjectSettings("Project/ABC");

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/ABC", SettingsScope.Project)
            {
                label = "ABC",
                activateHandler = (_, root) => BuildSettings(root),
                keywords = new HashSet<string> { "ABC", "Features", "Namespace", "Folder", "Scaffold" }
            };
        }

        internal static void BuildSettings(VisualElement container)
        {
            var root = ActorEditorStyles.Root(container);
            root.Add(ActorEditorStyles.Header("Project Setup", "ABC fits your project, not the other way around."));
            var scroll = ActorEditorStyles.Scroll(root);
            var card = ActorEditorStyles.Card("Source defaults", "No mandatory gameplay folders. Existing output folders follow their GUID when moved in Unity.");
            var settings = instance;
            var folder = new TextField("Feature folder") { value = settings.FeatureFolder };
            var namespaceName = new TextField("Namespace") { value = settings.FeatureNamespace };
            card.Add(folder);
            card.Add(namespaceName);
            var validation = new HelpBox("", HelpBoxMessageType.Warning);
            card.Add(validation);
            var actions = ActorEditorStyles.Row();
            var save = ActorEditorStyles.Button("Save project defaults", () =>
            {
                settings.SaveDefaults(folder.value, namespaceName.value);
                folder.SetValueWithoutNotify(settings.FeatureFolder);
                namespaceName.SetValueWithoutNotify(settings.FeatureNamespace);
            }, true);
            actions.Add(save);
            var selected = ActorEditorStyles.Button("Use selected folder", () =>
            {
                var path = ActorFeatureWizard.GetSelectedFolder();
                if (path != null)
                    folder.value = path;
            });
            actions.Add(selected);
            card.Add(actions);
            card.Add(ActorEditorStyles.Text("Saved to ProjectSettings/ABC.asset. Commit this file with the project. Saving does not create or move gameplay assets.", "abc-muted"));
            card.Add(ActorEditorStyles.Button("Open Feature Scaffold", ActorFeatureWizard.Open));
            scroll.Add(card);
            void Validate()
            {
                var error = ActorFeatureWriter.ValidateDefaults(folder.value, namespaceName.value);
                validation.text = error ?? string.Empty;
                validation.style.display = error == null ? DisplayStyle.None : DisplayStyle.Flex;
                save.SetEnabled(error == null);
            }
            void Reload()
            {
                folder.SetValueWithoutNotify(settings.FeatureFolder);
                namespaceName.SetValueWithoutNotify(settings.FeatureNamespace);
                Validate();
            }
            folder.RegisterValueChangedCallback(_ => Validate());
            namespaceName.RegisterValueChangedCallback(_ => Validate());
            root.RegisterCallback<AttachToPanelEvent>(_ => Undo.undoRedoPerformed += Reload);
            root.RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= Reload);
            root.schedule.Execute(() => selected.SetEnabled(ActorFeatureWizard.GetSelectedFolder() != null)).Every(500);
            Validate();
        }
    }
}
