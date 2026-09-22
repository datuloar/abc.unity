using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Editor
{
    internal sealed class ActorDashboardWindow : EditorWindow
    {
        private const string DocumentationUrl = "https://github.com/datuloar/abc.unity";
        private static readonly string QuickStart =
            "using var world = new ActorWorld(\"Combat\", 1000);\n" +
            "var actor = new ActorModel(\"Enemy\");\n" +
            "actor.AddData(new HealthData());\n" +
            "world.Add(actor);\n" +
            "world.Query<HealthData>().For(static (model, health) => health.Value++);";

        private Actor[] _sceneActors = System.Array.Empty<Actor>();
        private readonly List<ActorWorld> _worlds = new List<ActorWorld>();
        private Vector2 _scroll;
        private int _initializedCount;
        private int _aliveCount;

        [MenuItem("Tools/ABC/Dashboard", priority = 1200)]
        public static void Open()
        {
            var window = GetWindow<ActorDashboardWindow>();
            window.titleContent = new GUIContent("ABC", EditorGUIUtility.IconContent("d_GameObject Icon").image);
            window.minSize = new Vector2(420f, 430f);
            window.Show();
        }

        [MenuItem("GameObject/ABC/Actor", false, 10)]
        private static void CreateActor(MenuCommand command)
        {
            var gameObject = new GameObject("Actor");
            Undo.RegisterCreatedObjectUndo(gameObject, "Create ABC Actor");
            GameObjectUtility.SetParentAndAlign(gameObject, command.context as GameObject);
            Undo.AddComponent<Actor>(gameObject);
            Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);
        }

        [MenuItem("GameObject/ABC/Actor World Runner", false, 11)]
        private static void CreateWorldRunner(MenuCommand command)
        {
            var gameObject = new GameObject("Actor World");
            Undo.RegisterCreatedObjectUndo(gameObject, "Create ABC Actor World");
            GameObjectUtility.SetParentAndAlign(gameObject, command.context as GameObject);
            Undo.AddComponent<ActorWorldRunner>(gameObject);
            Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);
        }

        private void OnEnable()
        {
            EditorApplication.hierarchyChanged += Refresh;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            Refresh();
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= Refresh;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        }

        private void OnInspectorUpdate()
        {
            ActorWorldDiagnostics.GetWorlds(_worlds);
            UpdateRuntimeCounts();
            Repaint();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            ActorEditorStyles.DrawHeader("ABC Framework", "Fast composition for scene and simulation actors", "d_GameObject Icon");
            DrawSceneStatus();
            DrawWorldStatus();
            DrawActions();
            DrawQuickStart();
            EditorGUILayout.EndScrollView();
        }

        private void DrawSceneStatus()
        {
            ActorEditorStyles.BeginCard();
            GUILayout.Label("Scene Overview", ActorEditorStyles.Section);
            EditorGUILayout.BeginHorizontal();
            ActorEditorStyles.DrawMetric(_sceneActors.Length.ToString(), "Actors");
            ActorEditorStyles.DrawMetric(_initializedCount.ToString(), "Initialized");
            ActorEditorStyles.DrawMetric(_aliveCount.ToString(), "Alive");
            EditorGUILayout.EndHorizontal();

            if (_sceneActors.Length == 0)
                EditorGUILayout.HelpBox("No ABC actors are present in loaded scenes.", MessageType.Info);

            ActorEditorStyles.EndCard();
        }

        private void DrawActions()
        {
            ActorEditorStyles.BeginCard();
            GUILayout.Label("Create", ActorEditorStyles.Section);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Actor", ActorEditorStyles.CenteredButton))
                CreateActor(new MenuCommand(Selection.activeGameObject));

            if (GUILayout.Button("World Runner", ActorEditorStyles.CenteredButton))
                CreateWorldRunner(new MenuCommand(Selection.activeGameObject));

            if (GUILayout.Button("Blueprint", ActorEditorStyles.CenteredButton))
                CreateBlueprint();

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(3f);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Feature Scaffold"))
                ActorFeatureWizard.Open();

            using (new EditorGUI.DisabledScope(_sceneActors.Length == 0))
            {
                if (GUILayout.Button("Select Scene Actors"))
                    Selection.objects = _sceneActors;
            }

            if (GUILayout.Button("Documentation"))
                Application.OpenURL(DocumentationUrl);

            EditorGUILayout.EndHorizontal();
            ActorEditorStyles.EndCard();
        }

        private void DrawWorldStatus()
        {
            ActorWorldDiagnostics.GetWorlds(_worlds);
            var modelCount = 0;
            var indexedTypes = 0;

            for (var i = 0; i < _worlds.Count; i++)
            {
                modelCount += _worlds[i].Count;
                indexedTypes += _worlds[i].IndexedDataTypeCount;
            }

            ActorEditorStyles.BeginCard();
            GUILayout.Label("Simulation Worlds", ActorEditorStyles.Section);
            EditorGUILayout.BeginHorizontal();
            ActorEditorStyles.DrawMetric(_worlds.Count.ToString(), "Worlds");
            ActorEditorStyles.DrawMetric(modelCount.ToString(), "Models");
            ActorEditorStyles.DrawMetric(indexedTypes.ToString(), "Query Indexes");
            EditorGUILayout.EndHorizontal();

            if (_worlds.Count == 0)
            {
                EditorGUILayout.HelpBox("ActorWorld instances appear here automatically while they are alive.", MessageType.Info);
                ActorEditorStyles.EndCard();
                return;
            }

            for (var i = 0; i < _worlds.Count; i++)
                DrawWorldRow(_worlds[i]);

            ActorEditorStyles.EndCard();
        }

        private static void DrawWorldRow(ActorWorld world)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(world.Name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"{world.Count} / {world.Capacity}", EditorStyles.miniLabel, GUILayout.Width(88f));
            EditorGUILayout.EndHorizontal();

            var utilization = world.Capacity == 0 ? 0f : world.Count / (float)world.Capacity;
            var rect = GUILayoutUtility.GetRect(0f, 16f, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(rect, utilization, $"{utilization:P0} capacity · {world.IndexedDataTypeCount} indexed type(s)");
            EditorGUILayout.EndVertical();
        }

        private static void DrawQuickStart()
        {
            ActorEditorStyles.BeginCard();
            GUILayout.Label("Scene-free Simulation", ActorEditorStyles.Section);
            EditorGUILayout.HelpBox("ActorModel and ActorWorld use the same data, behaviours, commands, and lifecycle without requiring GameObjects.", MessageType.None);
            EditorGUILayout.TextArea(QuickStart, GUILayout.MinHeight(78f));
            ActorEditorStyles.EndCard();
        }

        private static void CreateBlueprint()
        {
            var blueprint = CreateInstance<ActorBlueprint>();
            ProjectWindowUtil.CreateAsset(blueprint, "New Actor Blueprint.asset");
        }

        private void Refresh()
        {
            var allActors = Resources.FindObjectsOfTypeAll<Actor>();
            var count = 0;

            for (var i = 0; i < allActors.Length; i++)
            {
                var actor = allActors[i];
                if (actor != null && !EditorUtility.IsPersistent(actor) && actor.gameObject.scene.IsValid())
                    count++;
            }

            _sceneActors = new Actor[count];
            var index = 0;

            for (var i = 0; i < allActors.Length; i++)
            {
                var actor = allActors[i];
                if (actor == null || EditorUtility.IsPersistent(actor) || !actor.gameObject.scene.IsValid())
                    continue;

                _sceneActors[index++] = actor;
            }

            UpdateRuntimeCounts();
            ActorWorldDiagnostics.GetWorlds(_worlds);
            Repaint();
        }

        private void UpdateRuntimeCounts()
        {
            _initializedCount = 0;
            _aliveCount = 0;

            for (var i = 0; i < _sceneActors.Length; i++)
            {
                var actor = _sceneActors[i];
                if (actor == null)
                    continue;

                if (actor.IsInitialized.Value)
                    _initializedCount++;
                if (actor.IsAlive.Value)
                    _aliveCount++;
            }
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            Refresh();
        }
    }
}
