using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Abc.Unity.Editor
{
    internal sealed class ActorFeatureWizard : EditorWindow
    {
        [SerializeField] private string _featureName = "Movement";
        [SerializeField] private string _namespaceName;
        [SerializeField] private string _folder;
        [SerializeField] private bool _createData = true;
        [SerializeField] private bool _createBehaviour = true;
        [SerializeField] private bool _createCommand;
        [SerializeField] private bool _createQueryAction;
        [SerializeField] private bool _createProviders = true;
        private TextField _folderField;
        private TextField _source;
        private PopupField<string> _filePicker;
        private Toggle _queryOption;
        private Toggle _providersOption;
        private HelpBox _validation;
        private Button _generate;
        private Button _saveDefaults;
        private Button _selectedFolder;
        private List<ActorFeatureFile> _files;

        [MenuItem("Tools/ABC/Feature Scaffold", priority = 1201)]
        internal static void Open()
        {
            var window = GetWindow<ActorFeatureWizard>();
            window.titleContent = new GUIContent("Feature Scaffold");
            window.minSize = new Vector2(380f, 500f);
            window.Show();
        }

        private void OnEnable()
        {
            var settings = ActorProjectSettings.instance;
            if (string.IsNullOrEmpty(_folder))
                _folder = settings.FeatureFolder;
            if (string.IsNullOrEmpty(_namespaceName))
                _namespaceName = settings.FeatureNamespace;
        }

        public void CreateGUI()
        {
            var root = ActorEditorStyles.Root(rootVisualElement);
            root.Add(ActorEditorStyles.Header("Feature Scaffold", "Your feature. Readable source. Zero runtime generator cost."));
            var scroll = ActorEditorStyles.Scroll(root);
            BuildIdentity(scroll);
            BuildOptions(scroll);
            BuildPreview(scroll);
            root.schedule.Execute(() => _selectedFolder.SetEnabled(GetSelectedFolder() != null)).Every(500);
            RefreshPreview();
        }

        private void BuildIdentity(VisualElement root)
        {
            var identity = ActorEditorStyles.Card("01  /  Identity & location");
            if (!ActorProjectSettings.instance.IsConfigured)
                identity.Add(new HelpBox("Choose your own folder and namespace. Save project defaults to reuse them for every feature.", HelpBoxMessageType.Info));
            var name = new TextField("Feature name") { value = _featureName };
            name.RegisterValueChangedCallback(evt => { _featureName = evt.newValue.Trim(); RefreshPreview(); });
            var namespaceName = new TextField("Namespace") { value = _namespaceName };
            namespaceName.RegisterValueChangedCallback(evt => { _namespaceName = evt.newValue.Trim(); RefreshPreview(); });
            _folderField = new TextField("Output folder") { value = _folder };
            _folderField.RegisterValueChangedCallback(evt => { _folder = evt.newValue.Trim(); RefreshPreview(); });
            identity.Add(name);
            identity.Add(namespaceName);
            identity.Add(_folderField);
            var actions = ActorEditorStyles.Row();
            actions.Add(ActorEditorStyles.Button("Browse", BrowseFolder));
            _selectedFolder = ActorEditorStyles.Button("Use selected folder", () =>
            {
                var folder = GetSelectedFolder();
                if (folder != null)
                    _folderField.value = folder;
            });
            actions.Add(_selectedFolder);
            _saveDefaults = ActorEditorStyles.Button("Save project defaults", () =>
            {
                ActorProjectSettings.instance.SaveDefaults(_folder, _namespaceName);
                ShowNotification(new GUIContent("Project defaults saved"));
            });
            actions.Add(_saveDefaults);
            identity.Add(actions);
            root.Add(identity);
        }

        private void BuildOptions(VisualElement root)
        {
            var options = ActorEditorStyles.Card("02  /  Composition", "Generate only what this feature needs.");
            AddOption(options, "Data", _createData, value => _createData = value);
            AddOption(options, "Behaviour with cached dependencies", _createBehaviour, value => _createBehaviour = value);
            AddOption(options, "Command and typed listener", _createCommand, value => _createCommand = value);
            _queryOption = AddOption(options, "Zero-boxing query action", _createQueryAction, value => _createQueryAction = value);
            _providersOption = AddOption(options, "Blueprint providers", _createProviders, value => _createProviders = value);
            root.Add(options);
        }

        private Toggle AddOption(VisualElement parent, string label, bool value, Action<bool> set)
        {
            var toggle = new Toggle { text = label, value = value };
            toggle.RegisterValueChangedCallback(evt => { set(evt.newValue); RefreshPreview(); });
            parent.Add(toggle);
            return toggle;
        }

        private void BuildPreview(VisualElement root)
        {
            var preview = ActorEditorStyles.Card("03  /  Review & generate", "Existing files are never overwritten. Generated code is yours to edit.");
            _filePicker = new PopupField<string>("Preview file", new List<string> { "No files" }, 0);
            _filePicker.RegisterValueChangedCallback(evt => UpdateSource());
            preview.Add(_filePicker);
            _source = new TextField { multiline = true, isReadOnly = true };
            _source.AddToClassList("abc-code");
            preview.Add(_source);
            _validation = new HelpBox("", HelpBoxMessageType.Warning);
            preview.Add(_validation);
            _generate = ActorEditorStyles.Button("Generate feature", Generate, true);
            preview.Add(_generate);
            root.Add(preview);
        }

        private void RefreshPreview()
        {
            if (_generate == null)
                return;
            _queryOption.SetEnabled(_createData);
            _providersOption.SetEnabled(_createData || _createBehaviour);
            _files = ActorFeatureTemplates.Build(_featureName, _namespaceName, _createData, _createBehaviour,
                _createCommand, _createQueryAction && _createData, _createProviders && (_createData || _createBehaviour));
            var error = Validate(_files);
            _validation.text = error ?? string.Empty;
            _validation.style.display = error != null ? DisplayStyle.Flex : DisplayStyle.None;
            _generate.SetEnabled(error == null);
            _generate.text = $"Generate {_files.Count} file(s)";
            _saveDefaults.SetEnabled(ActorFeatureWriter.ValidateDefaults(_folder, _namespaceName) == null);
            var choices = new List<string>(_files.Count);
            for (var i = 0; i < _files.Count; i++)
                choices.Add(_files[i].Name);
            if (choices.Count == 0)
                choices.Add("No files");
            var selected = Math.Max(0, choices.IndexOf(_filePicker.value));
            _filePicker.choices = choices;
            _filePicker.SetValueWithoutNotify(choices[selected]);
            _filePicker.SetEnabled(_files.Count > 0);
            UpdateSource();
        }

        private void UpdateSource()
        {
            var index = _filePicker.index;
            _source.SetValueWithoutNotify(_files != null && index >= 0 && index < _files.Count ? _files[index].Content : string.Empty);
        }

        private string Validate(IReadOnlyList<ActorFeatureFile> files) => ActorFeatureWriter.Validate(
            Path.GetFullPath(Path.Combine(Application.dataPath, "..")), _folder, _featureName, _namespaceName, files);

        private void Generate()
        {
            var error = Validate(_files);
            if (error != null)
            {
                RefreshPreview();
                return;
            }
            var folder = _folder.Replace('\\', '/').Trim().TrimEnd('/');
            try
            {
                WriteFiles(folder, _files);
            }
            catch (Exception exception) when (exception is InvalidOperationException ||
                                              exception is IOException || exception is UnauthorizedAccessException)
            {
                EditorUtility.DisplayDialog("ABC Feature Scaffold", exception.Message, "Close");
                return;
            }
            var firstAsset = AssetDatabase.LoadAssetAtPath<MonoScript>($"{folder}/{_files[0].Name}");
            Selection.activeObject = firstAsset;
            EditorGUIUtility.PingObject(firstAsset);
            ShowNotification(new GUIContent($"Generated {_files.Count} files"));
            RefreshPreview();
        }

        private void WriteFiles(string folder, IReadOnlyList<ActorFeatureFile> files)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                ActorFeatureWriter.Write(Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                    folder, _featureName, _namespaceName, files);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        private void BrowseFolder()
        {
            var selected = EditorUtility.OpenFolderPanel("ABC Feature Folder", Application.dataPath, string.Empty);
            if (string.IsNullOrEmpty(selected))
                return;
            var assetsRoot = Path.GetFullPath(Application.dataPath);
            var absolute = Path.GetFullPath(selected);
            var comparison = Application.platform == RuntimePlatform.WindowsEditor
                ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (string.Equals(absolute, assetsRoot, comparison))
            {
                _folderField.value = "Assets";
                return;
            }
            var prefix = assetsRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!absolute.StartsWith(prefix, comparison))
            {
                EditorUtility.DisplayDialog("ABC Feature Scaffold", "Select a folder inside this project's Assets folder.", "Close");
                return;
            }
            _folderField.value = "Assets/" + absolute.Substring(prefix.Length).Replace('\\', '/');
        }

        internal static string GetSelectedFolder()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
                return null;
            if (!AssetDatabase.IsValidFolder(path))
                path = Path.GetDirectoryName(path)?.Replace('\\', '/');
            return path == "Assets" || path != null && path.StartsWith("Assets/", StringComparison.Ordinal) ? path : null;
        }
    }
}
