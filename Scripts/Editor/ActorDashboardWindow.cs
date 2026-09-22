using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Abc.Unity.Editor
{
    internal sealed class ActorDashboardWindow : EditorWindow
    {
        private const string DocumentationUrl = "https://github.com/datuloar/abc.unity";
        private readonly List<ActorWorld> _worlds = new List<ActorWorld>();
        private readonly List<ActorWorld> _displayedWorlds = new List<ActorWorld>();
        private readonly List<Actor> _sceneActors = new List<Actor>();
        private readonly List<ProgressBar> _worldCapacity = new List<ProgressBar>();
        private VisualElement _worldRows;
        private Label _actorCount;
        private Label _aliveCount;
        private Label _worldCount;
        private Label _modelCount;

        [MenuItem("Tools/ABC/Dashboard", priority = 1200)]
        public static void Open()
        {
            var window = GetWindow<ActorDashboardWindow>();
            window.titleContent = new GUIContent("ABC");
            window.minSize = new Vector2(380f, 440f);
            window.Show();
        }

        public void CreateGUI()
        {
            var root = ActorEditorStyles.Root(rootVisualElement);
            root.Add(ActorEditorStyles.Header("Build gameplay, not plumbing.", "One composition model. Free tools. From prototype to simulation."));
            var scroll = ActorEditorStyles.Scroll(root);
            BuildActions(scroll);
            var overview = ActorEditorStyles.Card("Live workspace");
            var metrics = ActorEditorStyles.Row("abc-metrics");
            _actorCount = ActorEditorStyles.Metric(metrics, "Scene actors");
            _aliveCount = ActorEditorStyles.Metric(metrics, "Alive");
            _worldCount = ActorEditorStyles.Metric(metrics, "Worlds");
            _modelCount = ActorEditorStyles.Metric(metrics, "Models");
            overview.Add(metrics);
            overview.Add(ActorEditorStyles.Button("Open World Explorer", ActorWorldExplorerWindow.Open, true));
            scroll.Add(overview);
            _worldRows = new VisualElement();
            scroll.Add(_worldRows);
            _displayedWorlds.Clear();
            _worldCapacity.Clear();
            BuildQuickStart(scroll);
            root.schedule.Execute(Refresh).Every(1000);
            Refresh();
        }

        private static void BuildActions(VisualElement root)
        {
            var create = ActorEditorStyles.Card("Start with a feature", "Generate ordinary, editable C#. Choose your own folders and namespace.");
            var source = ActorEditorStyles.Row();
            source.Add(ActorEditorStyles.Button("Feature Scaffold", ActorFeatureWizard.Open, true));
            source.Add(ActorEditorStyles.Button("Project Setup", ActorProjectSettings.Open));
            source.Add(ActorEditorStyles.Button("Documentation", () => Application.OpenURL(DocumentationUrl)));
            create.Add(source);
            var scene = ActorEditorStyles.Row();
            scene.Add(ActorEditorStyles.Button("Create Actor", () => CreateActor(new MenuCommand(Selection.activeGameObject))));
            scene.Add(ActorEditorStyles.Button("Create World Runner", () => CreateWorldRunner(new MenuCommand(Selection.activeGameObject))));
            scene.Add(ActorEditorStyles.Button("Create Blueprint", CreateBlueprint));
            create.Add(scene);
            root.Add(create);
        }

        private static void BuildQuickStart(VisualElement root)
        {
            var card = ActorEditorStyles.Card("A small model. A scalable world.", "Use the same data and behaviours with or without GameObjects.");
            var code = new TextField
            {
                isReadOnly = true,
                multiline = true,
                value = "using var world = new ActorWorld(\"Combat\", 1000);\n" +
                    "var enemy = new ActorModel(\"Enemy\")\n" +
                    "    .WithData(new HealthData());\n" +
                    "world.Add(enemy);\n" +
                    "world.Query<HealthData>().For(static (model, health) => health.Value++);"
            };
            code.AddToClassList("abc-code");
            card.Add(code);
            card.Add(ActorEditorStyles.Text("HealthData is your gameplay state. Cache dependencies during Initialize; use indexed queries where scale requires them.", "abc-muted"));
            root.Add(card);
        }

        private void Refresh()
        {
            _sceneActors.Clear();
            var actors = Resources.FindObjectsOfTypeAll<Actor>();
            var alive = 0;
            for (var i = 0; i < actors.Length; i++)
            {
                var actor = actors[i];
                if (actor == null || EditorUtility.IsPersistent(actor) || !actor.gameObject.scene.IsValid())
                    continue;
                _sceneActors.Add(actor);
                if (actor.IsAlive.Value)
                    alive++;
            }
            ActorWorldDiagnostics.GetWorlds(_worlds);
            var models = 0;
            for (var i = 0; i < _worlds.Count; i++)
                models += _worlds[i].Count;
            _actorCount.text = _sceneActors.Count.ToString();
            _aliveCount.text = alive.ToString();
            _worldCount.text = _worlds.Count.ToString();
            _modelCount.text = models.ToString("N0");
            RefreshWorldRows();
        }

        private void RefreshWorldRows()
        {
            var changed = _worlds.Count != _displayedWorlds.Count;
            for (var i = 0; !changed && i < _worlds.Count; i++)
                changed = !ReferenceEquals(_worlds[i], _displayedWorlds[i]);
            if (changed || _worldRows.childCount == 0)
                BuildWorldRows();
            for (var i = 0; i < _worlds.Count; i++)
            {
                var world = _worlds[i];
                _worldCapacity[i].value = world.Capacity == 0 ? 0f : 100f * world.Count / world.Capacity;
                _worldCapacity[i].title = $"{world.Count:N0} / {world.Capacity:N0} models  ·  {world.IndexedDataTypeCount} indexes";
            }
        }

        private void BuildWorldRows()
        {
            _worldRows.Clear();
            _worldCapacity.Clear();
            _displayedWorlds.Clear();
            _displayedWorlds.AddRange(_worlds);
            if (_worlds.Count == 0)
                _worldRows.Add(ActorEditorStyles.Card("Ready when you are", "Live worlds appear here automatically. Open Bot Arena or create your first ActorWorld."));
            for (var i = 0; i < _worlds.Count; i++)
            {
                var world = _worlds[i];
                var card = ActorEditorStyles.Card(world.Name);
                var capacity = new ProgressBar();
                card.Add(capacity);
                card.Add(ActorEditorStyles.Button("Inspect models", () => ActorWorldExplorerWindow.OpenWorld(world)));
                _worldRows.Add(card);
                _worldCapacity.Add(capacity);
            }
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

        private static void CreateBlueprint()
        {
            var blueprint = CreateInstance<ActorBlueprint>();
            ProjectWindowUtil.CreateAsset(blueprint, "New Actor Blueprint.asset");
        }
    }
}
