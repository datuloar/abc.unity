using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEditor;
using UnityEngine;

namespace Abc.Unity.Editor
{
    internal sealed class ActorFeatureWizard : EditorWindow
    {
        private const string DefaultFolder = "Assets/Game/Features";
        private const string DefaultNamespace = "Game.Features";

        private static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        };

        private string _featureName = "Movement";
        private string _namespaceName = DefaultNamespace;
        private string _folder = DefaultFolder;
        private bool _createData = true;
        private bool _createBehaviour = true;
        private bool _createCommand;
        private bool _createQueryAction;
        private bool _createProviders = true;
        private Vector2 _scroll;

        [MenuItem("Tools/ABC/Feature Scaffold", priority = 1201)]
        internal static void Open()
        {
            var window = GetWindow<ActorFeatureWizard>();
            window.titleContent = new GUIContent("ABC Feature", EditorGUIUtility.IconContent("d_cs Script Icon").image);
            window.minSize = new Vector2(460f, 520f);
            window.Show();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            ActorEditorStyles.DrawHeader("Feature Scaffold", "Readable source with zero generator runtime cost", "d_cs Script Icon");
            DrawIdentity();
            DrawModules();
            DrawPreviewAndGenerate();
            EditorGUILayout.EndScrollView();
        }

        private void DrawIdentity()
        {
            ActorEditorStyles.BeginCard();
            GUILayout.Label("Identity", ActorEditorStyles.Section);
            _featureName = EditorGUILayout.TextField("Feature Name", _featureName).Trim();
            _namespaceName = EditorGUILayout.TextField("Namespace", _namespaceName).Trim();

            EditorGUILayout.BeginHorizontal();
            _folder = EditorGUILayout.TextField("Output Folder", _folder).Trim();
            if (GUILayout.Button("Browse", GUILayout.Width(70f)))
                BrowseFolder();
            EditorGUILayout.EndHorizontal();
            ActorEditorStyles.EndCard();
        }

        private void DrawModules()
        {
            ActorEditorStyles.BeginCard();
            GUILayout.Label("Generate", ActorEditorStyles.Section);
            _createData = EditorGUILayout.ToggleLeft("Data", _createData);
            _createBehaviour = EditorGUILayout.ToggleLeft("Behaviour with cached dependencies", _createBehaviour);
            _createCommand = EditorGUILayout.ToggleLeft("Command and behaviour listener", _createCommand);

            using (new EditorGUI.DisabledScope(!_createData))
                _createQueryAction = EditorGUILayout.ToggleLeft("Zero-boxing query action", _createQueryAction && _createData);

            using (new EditorGUI.DisabledScope(!_createData && !_createBehaviour))
                _createProviders = EditorGUILayout.ToggleLeft("Blueprint providers", _createProviders && (_createData || _createBehaviour));

            EditorGUILayout.HelpBox(
                "Files are deterministic, editable C# source. The scaffold adds no runtime service, generated DLL, analyzer, or player dependency.",
                MessageType.None);
            ActorEditorStyles.EndCard();
        }

        private void DrawPreviewAndGenerate()
        {
            var files = BuildFiles();
            var error = Validate(files);

            ActorEditorStyles.BeginCard();
            GUILayout.Label($"Preview  {files.Count} file(s)", ActorEditorStyles.Section);

            if (files.Count == 0)
                EditorGUILayout.HelpBox("Select at least one feature module.", MessageType.Info);
            else
                DrawFileList(files);

            if (!string.IsNullOrEmpty(error))
                EditorGUILayout.HelpBox(error, MessageType.Error);

            using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(error)))
            {
                if (GUILayout.Button("Generate Feature", ActorEditorStyles.CenteredButton))
                    Generate(files);
            }

            ActorEditorStyles.EndCard();
        }

        private static void DrawFileList(IReadOnlyList<ActorFeatureFile> files)
        {
            for (var i = 0; i < files.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(EditorGUIUtility.IconContent("cs Script Icon"), GUILayout.Width(22f));
                GUILayout.Label(files[i].Name, EditorStyles.label);
                EditorGUILayout.EndHorizontal();
            }
        }

        private List<ActorFeatureFile> BuildFiles() => ActorFeatureTemplates.Build(
            _featureName,
            _namespaceName,
            _createData,
            _createBehaviour,
            _createCommand,
            _createQueryAction,
            _createProviders);

        private string Validate(IReadOnlyList<ActorFeatureFile> files)
        {
            if (files.Count == 0)
                return "Select at least one feature module.";
            if (!IsIdentifier(_featureName))
                return "Feature Name must be a valid C# identifier.";
            if (!IsNamespace(_namespaceName))
                return "Namespace must contain valid dot-separated C# identifiers.";

            var folder = NormalizeFolder(_folder);
            if (folder != "Assets" && !folder.StartsWith("Assets/", StringComparison.Ordinal))
                return "Output Folder must be inside Assets.";
            if (folder.Contains(".."))
                return "Output Folder cannot contain parent traversal.";

            for (var i = 0; i < files.Count; i++)
            {
                var assetPath = $"{folder}/{files[i].Name}";
                if (File.Exists(ToAbsolutePath(assetPath)))
                    return $"{assetPath} already exists. Existing source is never overwritten.";
            }

            return null;
        }

        private void Generate(IReadOnlyList<ActorFeatureFile> files)
        {
            var error = Validate(files);
            if (!string.IsNullOrEmpty(error))
            {
                EditorUtility.DisplayDialog("ABC Feature Scaffold", error, "Close");
                return;
            }

            var folder = NormalizeFolder(_folder);
            Directory.CreateDirectory(ToAbsolutePath(folder));
            AssetDatabase.StartAssetEditing();

            try
            {
                for (var i = 0; i < files.Count; i++)
                {
                    var path = ToAbsolutePath($"{folder}/{files[i].Name}");
                    File.WriteAllText(path, files[i].Content, new UTF8Encoding(false));
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            var firstAssetPath = $"{folder}/{files[0].Name}";
            var firstAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(firstAssetPath);
            Selection.activeObject = firstAsset;
            EditorGUIUtility.PingObject(firstAsset);
            ShowNotification(new GUIContent($"Generated {files.Count} files"));
        }

        private void BrowseFolder()
        {
            var selected = EditorUtility.OpenFolderPanel("ABC Feature Folder", Application.dataPath, string.Empty);
            if (string.IsNullOrEmpty(selected))
                return;

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var absolute = Path.GetFullPath(selected);
            if (!absolute.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("ABC Feature Scaffold", "Select a folder inside this Unity project.", "Close");
                return;
            }

            _folder = NormalizeFolder(absolute.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        private static bool IsNamespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var parts = value.Split('.');
            for (var i = 0; i < parts.Length; i++)
            {
                if (!IsIdentifier(parts[i]))
                    return false;
            }

            return true;
        }

        private static bool IsIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || Keywords.Contains(value))
                return false;
            if (value[0] != '_' && !char.IsLetter(value[0]))
                return false;

            for (var i = 1; i < value.Length; i++)
            {
                if (value[i] != '_' && !char.IsLetterOrDigit(value[i]))
                    return false;
            }

            return true;
        }

        private static string NormalizeFolder(string value) => value.Replace('\\', '/').Trim().TrimEnd('/');

        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }
    }
}
