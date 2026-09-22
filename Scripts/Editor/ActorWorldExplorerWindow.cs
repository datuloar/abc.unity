using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Abc.Unity.Editor
{
    internal sealed partial class ActorWorldExplorerWindow : EditorWindow
    {
        private readonly List<ActorWorld> _worlds = new List<ActorWorld>();
        private readonly List<ActorModel> _models = new List<ActorModel>();
        private readonly List<ActorModel> _matchedModels = new List<ActorModel>();
        private readonly List<Actor> _sceneActors = new List<Actor>();
        private readonly List<Actor> _matchedSceneActors = new List<Actor>();
        private readonly List<IActorModule> _modules = new List<IActorModule>();
        private readonly ActorWorldExplorerSearch _search = new ActorWorldExplorerSearch();
        private ActorWorld _selectedWorld;
        private ActorModel _selectedModel;
        private Actor _selectedSceneActor;
        [SerializeField] private string _query = string.Empty;
        [SerializeField] private int _tab;
        [SerializeField] private bool _showPrivateFields;
        [SerializeField] private bool _aliveOnly;
        [SerializeField] private bool _autoRefresh = true;
        private ListView _list;
        private ToolbarSearchField _searchField;
        private PopupField<string> _worldSelector;
        private Button _worldTab;
        private Button _sceneTab;
        private Label _summary;
        private Label _resultCount;
        private Label _emptyResults;
        private IVisualElementScheduledItem _filterTask;

        [MenuItem("Tools/ABC/World Explorer", priority = 1202)]
        public static void Open()
        {
            var window = GetWindow<ActorWorldExplorerWindow>();
            window.titleContent = new GUIContent("World Explorer");
            window.minSize = new Vector2(420f, 560f);
            window.Show();
        }

        internal static void OpenWorld(ActorWorld world)
        {
            Open();
            var window = GetWindow<ActorWorldExplorerWindow>();
            window._selectedWorld = world;
            window.ChangeTab(0);
            window.Focus();
        }

        internal static void OpenActor(Actor actor)
        {
            Open();
            var window = GetWindow<ActorWorldExplorerWindow>();
            window._selectedSceneActor = actor;
            window.ChangeTab(1);
            window._list.ScrollToItem(window._matchedSceneActors.IndexOf(actor));
            window.Focus();
        }

        private void OnEnable() => EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            _filterTask?.Pause();
            _worlds.Clear();
            _models.Clear();
            _matchedModels.Clear();
            _sceneActors.Clear();
            _matchedSceneActors.Clear();
            _modules.Clear();
            _detailModules.Clear();
            _selectedWorld = null;
            _selectedModel = null;
            _selectedSceneActor = null;
            _detailOwner = null;
            _selectedModule = null;
        }

        public void CreateGUI()
        {
            var root = ActorEditorStyles.Root(rootVisualElement);
            root.EnableInClassList("abc-narrow", position.width < 720f);
            root.Add(ActorEditorStyles.Header("World Explorer", "Live composition. Read-only inspection. One clear view."));
            BuildNavigation(root);
            BuildSearch(root);
            BuildWorkspace(root);
            root.RegisterCallback<GeometryChangedEvent>(evt =>
                root.EnableInClassList("abc-narrow", evt.newRect.width < 720f));
            root.RegisterCallback<KeyDownEvent>(HandleKeyboard);
            root.schedule.Execute(() => { if (_autoRefresh) Refresh(); }).Every(1000);
            _search.SetQuery(_query);
            Refresh();
        }

        private void BuildNavigation(VisualElement root)
        {
            var navigation = ActorEditorStyles.Row("abc-toolbar");
            _worldTab = ActorEditorStyles.Button("Simulation Worlds", () => ChangeTab(0));
            _sceneTab = ActorEditorStyles.Button("Scene Actors", () => ChangeTab(1));
            navigation.Add(_worldTab);
            navigation.Add(_sceneTab);
            var automatic = new Toggle { text = "Live", value = _autoRefresh, tooltip = "Refresh once per second. Disable to inspect a snapshot." };
            automatic.RegisterValueChangedCallback(evt => _autoRefresh = evt.newValue);
            navigation.Add(automatic);
            navigation.Add(ActorEditorStyles.Button("Refresh", Refresh));
            root.Add(navigation);
            var context = ActorEditorStyles.Row("abc-toolbar");
            _worldSelector = new PopupField<string>("World", new List<string> { "No worlds" }, 0);
            _worldSelector.style.flexGrow = 1;
            _worldSelector.RegisterValueChangedCallback(evt =>
            {
                var index = _worldSelector.index;
                if (index >= 0 && index < _worlds.Count && !ReferenceEquals(_selectedWorld, _worlds[index]))
                {
                    _selectedWorld = _worlds[index];
                    _selectedModel = null;
                    Refresh();
                }
            });
            context.Add(_worldSelector);
            _summary = ActorEditorStyles.Text("", "abc-pill");
            context.Add(_summary);
            root.Add(context);
        }

        private void BuildSearch(VisualElement root)
        {
            var toolbar = ActorEditorStyles.Row("abc-toolbar");
            _searchField = new ToolbarSearchField { value = _query, name = "WorldExplorerSearch" };
            _searchField.AddToClassList("abc-search");
            _searchField.tooltip = "Search names, tags or modules. Combine name:, tag:, data: and behaviour:. Ctrl/Cmd+F focuses search.";
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _query = evt.newValue;
                _filterTask?.Pause();
                _filterTask = root.schedule.Execute(() => { _search.SetQuery(_query); ApplyFilters(); }).StartingIn(150);
            });
            toolbar.Add(_searchField);
            var alive = new Toggle { text = "Alive only", value = _aliveOnly };
            alive.RegisterValueChangedCallback(evt => { _aliveOnly = evt.newValue; ApplyFilters(); });
            toolbar.Add(alive);
            root.Add(toolbar);
        }

        private void ChangeTab(int tab)
        {
            _tab = tab;
            _detailOwner = null;
            Refresh();
        }

        private void HandleKeyboard(KeyDownEvent evt)
        {
            if (!(evt.ctrlKey || evt.commandKey) || evt.keyCode != KeyCode.F)
                return;
            _searchField.Focus();
            evt.StopPropagation();
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange state) => Refresh();

        private void Refresh()
        {
            if (_list == null)
                return;
            ActorWorldDiagnostics.GetWorlds(_worlds);
            if (_selectedWorld == null || !_worlds.Contains(_selectedWorld))
                _selectedWorld = _worlds.Count > 0 ? _worlds[0] : null;
            _worldTab.EnableInClassList("abc-selected", _tab == 0);
            _sceneTab.EnableInClassList("abc-selected", _tab == 1);
            _worldSelector.style.display = _tab == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (_tab == 0)
                RefreshWorld();
            else
                RefreshScene();
            ApplyFilters();
        }

        private void RefreshWorld()
        {
            var labels = new List<string>(_worlds.Count);
            for (var i = 0; i < _worlds.Count; i++)
                labels.Add($"{i + 1}. {_worlds[i].Name}");
            if (labels.Count == 0)
                labels.Add("No simulation worlds");
            _worldSelector.choices = labels;
            _worldSelector.SetValueWithoutNotify(labels[Math.Max(0, _worlds.IndexOf(_selectedWorld))]);
            _worldSelector.SetEnabled(_worlds.Count > 0);
            _models.Clear();
            _selectedWorld?.CopyModelsTo(_models);
            _summary.text = _selectedWorld == null ? "Create a world to begin" :
                $"{_selectedWorld.Count:N0} models  /  {_selectedWorld.Capacity:N0} capacity  /  {_selectedWorld.IndexedDataTypeCount} indexes";
        }

        private void RefreshScene()
        {
            _sceneActors.Clear();
            var actors = Resources.FindObjectsOfTypeAll<Actor>();
            for (var i = 0; i < actors.Length; i++)
            {
                var actor = actors[i];
                if (actor != null && !EditorUtility.IsPersistent(actor) && actor.gameObject.scene.IsValid())
                    _sceneActors.Add(actor);
            }
            _summary.text = $"{_sceneActors.Count:N0} actors in loaded scenes";
        }

        private void ApplyFilters()
        {
            if (_list == null)
                return;
            _matchedModels.Clear();
            _matchedSceneActors.Clear();
            if (_tab == 0)
            {
                for (var i = 0; i < _models.Count; i++)
                {
                    var model = _models[i];
                    if (_aliveOnly && !model.IsAliveValue)
                        continue;
                    if (_search.RequiresModules)
                        model.CopyModulesTo(_modules);
                    if (_search.Matches(model, _search.RequiresModules ? _modules : null))
                        _matchedModels.Add(model);
                }
            }
            else
            {
                for (var i = 0; i < _sceneActors.Count; i++)
                {
                    var actor = _sceneActors[i];
                    if (actor == null || _aliveOnly && !actor.IsAlive.Value)
                        continue;
                    if (_search.RequiresModules)
                        actor.CopyModulesTo(_modules);
                    if (_search.Matches(actor.Name, actor.Tag.Value, _search.RequiresModules ? _modules : null))
                        _matchedSceneActors.Add(actor);
                }
            }
            UpdateResults();
        }

        private void UpdateResults()
        {
            IList source = _tab == 0 ? (IList)_matchedModels : _matchedSceneActors;
            var index = _tab == 0 ? _matchedModels.IndexOf(_selectedModel) : _matchedSceneActors.IndexOf(_selectedSceneActor);
            if (index < 0 && source.Count > 0)
                index = 0;
            _selectedModel = _tab == 0 && index >= 0 ? _matchedModels[index] : null;
            _selectedSceneActor = _tab == 1 && index >= 0 ? _matchedSceneActors[index] : null;
            _list.itemsSource = source;
            _list.RefreshItems();
            _list.SetSelectionWithoutNotify(index >= 0 ? new[] { index } : Array.Empty<int>());
            _resultCount.text = $"{source.Count:N0} matching {(_tab == 0 ? "models" : "actors")}  ·  ↑ ↓ to navigate";
            _emptyResults.style.display = source.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _emptyResults.text = _query.Length > 0 || _aliveOnly
                ? "No matches. Clear the search or disable Alive only."
                : _tab == 0 ? "No models yet. Enter Play Mode with an ActorWorld, or switch to Scene Actors."
                : "No actors in loaded scenes. Create one with GameObject > ABC > Actor.";
            UpdateDetails();
        }
    }
}
