using System;
using System.Collections.Generic;
using System.Text;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Abc.Unity.Editor
{
    internal sealed partial class ActorWorldExplorerWindow
    {
        private readonly List<IActorModule> _detailModules = new List<IActorModule>();
        private object _detailOwner;
        private IActorModule _selectedModule;
        private ScrollView _details;
        private VisualElement _moduleButtons;
        private VisualElement _values;
        private VisualElement _valueCard;
        private Label _identity;
        private Label _state;
        private Label _composition;
        private Label _moduleTitle;
        private PopupField<string> _modulePicker;

        private void BuildWorkspace(VisualElement root)
        {
            var workspace = new VisualElement();
            workspace.AddToClassList("abc-workspace");
            var results = new VisualElement();
            results.AddToClassList("abc-results");
            _resultCount = ActorEditorStyles.Text("", "abc-results-count");
            results.Add(_resultCount);
            _list = new ListView
            {
                fixedItemHeight = 58,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                selectionType = SelectionType.Single,
                makeItem = MakeRow,
                bindItem = BindRow,
                name = "ActorResults",
                viewDataKey = "abc-explorer-results"
            };
            _list.AddToClassList("abc-list");
            _list.selectionChanged += SelectResult;
            results.Add(_list);
            _emptyResults = ActorEditorStyles.Text("", "abc-empty");
            results.Add(_emptyResults);
            workspace.Add(results);
            _details = ActorEditorStyles.Scroll(workspace);
            _details.AddToClassList("abc-detail");
            root.Add(workspace);
        }

        private static VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("abc-list-row");
            row.Add(ActorEditorStyles.Text("", "abc-list-name"));
            row.Add(ActorEditorStyles.Text("", "abc-list-meta"));
            return row;
        }

        private void BindRow(VisualElement row, int index)
        {
            if (_tab == 0)
            {
                var model = _matchedModels[index];
                ((Label)row[0]).text = model.Name;
                ((Label)row[1]).text = $"{model.TagValue}  ·  {(model.IsAliveValue ? "Alive" : model.LifecycleState)}  ·  {model.DataCount}D / {model.BehaviourCount}B";
                row.tooltip = model.Name;
            }
            else
            {
                var actor = _matchedSceneActors[index];
                if (actor == null)
                    return;
                ((Label)row[0]).text = actor.Name;
                ((Label)row[1]).text = $"{actor.Tag.Value}  ·  {actor.LifecycleState}  ·  {actor.gameObject.scene.name}";
                row.tooltip = actor.Name;
            }
        }

        private void SelectResult(IEnumerable<object> selection)
        {
            foreach (var item in selection)
            {
                _selectedModel = item as ActorModel;
                _selectedSceneActor = item as Actor;
                UpdateDetails();
                break;
            }
        }

        private void UpdateDetails()
        {
            object owner = _tab == 0 ? (object)_selectedModel : _selectedSceneActor;
            _modules.Clear();
            if (_tab == 0)
                _selectedModel?.CopyModulesTo(_modules);
            else if (_selectedSceneActor != null)
                _selectedSceneActor.CopyModulesTo(_modules);
            if (_details.childCount == 0 || !ReferenceEquals(owner, _detailOwner) || !SameModules())
            {
                _detailOwner = owner;
                _detailModules.Clear();
                _detailModules.AddRange(_modules);
                _selectedModule = _detailModules.Count > 0 ? _detailModules[0] : null;
                BuildDetails();
            }
            if (owner == null)
                return;
            UpdateIdentity();
            ActorWorldExplorerFields.Refresh(_values, _selectedModule);
        }

        private bool SameModules()
        {
            if (_modules.Count != _detailModules.Count)
                return false;
            for (var i = 0; i < _modules.Count; i++)
            {
                if (!ReferenceEquals(_modules[i], _detailModules[i]))
                    return false;
            }
            return true;
        }

        private void BuildDetails()
        {
            _details.Clear();
            _details.scrollOffset = Vector2.zero;
            if (_detailOwner == null)
            {
                _details.Add(ActorEditorStyles.Header("Composition", "Select an actor or model to see what it is made of."));
                _details.Add(ActorEditorStyles.Text("Inspect data, behaviours and live fields. Copy a readable snapshot for your team or AI agent.", "abc-empty"));
                return;
            }
            var identity = ActorEditorStyles.Card();
            _identity = ActorEditorStyles.Text("", "abc-title");
            _state = ActorEditorStyles.Text("", "abc-pill");
            _composition = ActorEditorStyles.Text("", "abc-muted");
            identity.Add(_identity);
            identity.Add(_state);
            identity.Add(_composition);
            var actions = ActorEditorStyles.Row();
            actions.Add(ActorEditorStyles.Button("Copy snapshot", CopySnapshot));
            if (_tab == 1)
                actions.Add(ActorEditorStyles.Button("Select in Hierarchy", SelectSceneObject));
            identity.Add(actions);
            _details.Add(identity);
            BuildModuleNavigation();
            BuildValueCard();
            _details.Add(_moduleButtons);
        }

        private void UpdateIdentity()
        {
            var model = _selectedModel;
            var actor = _selectedSceneActor;
            _identity.text = _tab == 0 ? model.Name : actor.Name;
            var tag = _tab == 0 ? model.TagValue : actor.Tag.Value;
            var lifecycle = _tab == 0 ? model.LifecycleState : actor.LifecycleState;
            var alive = _tab == 0 ? model.IsAliveValue : actor.IsAlive.Value;
            _state.text = $"{(alive ? "Alive" : "Not alive")}  ·  {lifecycle}  ·  {tag} ({tag.Value})";
            _composition.text = _tab == 0
                ? $"{model.DataCount} data  /  {model.BehaviourCount} behaviours  ·  {(model.IsPendingInWorld ? "Pending" : "Active")} in {_selectedWorld.Name}"
                : $"{actor.DataCount} data  /  {actor.BehaviourCount} behaviours  ·  {actor.gameObject.scene.name}";
        }

        private void BuildModuleNavigation()
        {
            _moduleButtons = ActorEditorStyles.Card("Composition", "Select a module to inspect its fields.");
            for (var i = 0; i < _detailModules.Count; i++)
            {
                var module = _detailModules[i];
                var data = module is IActorData;
                var button = ActorEditorStyles.Button($"{(data ? "DATA" : "LOGIC")}  /  {module.GetType().Name}",
                    () => SelectModule(module));
                button.userData = module;
                button.tooltip = module.GetType().FullName;
                button.AddToClassList("abc-module-button");
                button.AddToClassList(data ? "abc-module-data" : "abc-module-behaviour");
                _moduleButtons.Add(button);
            }
            if (_detailModules.Count == 0)
                _moduleButtons.Add(ActorEditorStyles.Text("No runtime modules. Scene actors populate after initialization.", "abc-muted"));
        }

        private void BuildValueCard()
        {
            _valueCard = ActorEditorStyles.Card();
            _moduleTitle = ActorEditorStyles.Text("", "abc-section");
            _valueCard.Add(_moduleTitle);
            var choices = new List<string>(_detailModules.Count);
            for (var i = 0; i < _detailModules.Count; i++)
                choices.Add($"{i + 1}. {_detailModules[i].GetType().Name}");
            if (choices.Count == 0)
                choices.Add("No modules");
            _modulePicker = new PopupField<string>("Inspect", choices, 0);
            _modulePicker.RegisterValueChangedCallback(_ =>
            {
                var index = _modulePicker.index;
                if (index >= 0 && index < _detailModules.Count)
                    SelectModule(_detailModules[index]);
            });
            _valueCard.Add(_modulePicker);
            var actions = ActorEditorStyles.Row();
            var fields = new Toggle { text = "Private fields", value = _showPrivateFields };
            fields.tooltip = "Includes private runtime fields. No property getters are executed.";
            fields.RegisterValueChangedCallback(evt =>
            {
                _showPrivateFields = evt.newValue;
                ActorWorldExplorerFields.Populate(_values, _selectedModule, _showPrivateFields);
            });
            actions.Add(fields);
            actions.Add(ActorEditorStyles.Button("Open source", OpenModuleSource));
            _valueCard.Add(actions);
            _valueCard.Add(ActorEditorStyles.Text("READ-ONLY  ·  Fields only; custom getters never execute", "abc-muted"));
            _values = new VisualElement();
            _valueCard.Add(_values);
            _details.Add(_valueCard);
            SelectModule(_selectedModule);
        }

        private void SelectModule(IActorModule module)
        {
            _selectedModule = module;
            _valueCard.style.display = module == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (module == null)
                return;
            _moduleTitle.text = module.GetType().Name;
            _modulePicker.SetValueWithoutNotify(_modulePicker.choices[_detailModules.IndexOf(module)]);
            foreach (var child in _moduleButtons.Children())
                child.EnableInClassList("abc-selected", ReferenceEquals(child.userData, module));
            ActorWorldExplorerFields.Populate(_values, module, _showPrivateFields);
        }

        private void OpenModuleSource()
        {
            if (_selectedModule != null && !ActorEditorSource.Open(_selectedModule.GetType()))
                ShowNotification(new GUIContent("Source is not available as a Unity script asset"));
        }

        private void SelectSceneObject()
        {
            if (_selectedSceneActor == null)
                return;
            Selection.activeGameObject = _selectedSceneActor.gameObject;
            EditorGUIUtility.PingObject(_selectedSceneActor.gameObject);
        }

        private void CopySnapshot()
        {
            if (_detailOwner == null)
                return;
            var report = new StringBuilder(256);
            report.Append("ABC ").Append(_tab == 0 ? "model" : "scene actor").Append(": ").AppendLine(_identity.text);
            report.AppendLine(_state.text).AppendLine(_composition.text).AppendLine("Modules:");
            for (var i = 0; i < _detailModules.Count; i++)
            {
                var module = _detailModules[i];
                report.Append("- ").AppendLine(module.GetType().FullName);
                ActorWorldExplorerFields.AppendSnapshot(report, module, _showPrivateFields);
            }
            EditorGUIUtility.systemCopyBuffer = report.ToString();
            ShowNotification(new GUIContent("Snapshot copied"));
        }
    }
}
