using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Abc.Unity.Editor
{
    internal sealed class ActorProviderPicker : EditorWindow
    {
        private readonly List<ActorProviderCatalog.Option> _filtered = new List<ActorProviderCatalog.Option>();
        private IReadOnlyList<ActorProviderCatalog.Option> _options;
        private Func<Type, bool> _contains;
        private Action<ActorProviderCatalog.Option> _add;
        private ListView _list;
        private Button _confirm;
        private Label _status;
        private ToolbarSearchField _search;
        private bool _data;

        internal static void Open(bool data, Func<Type, bool> contains, Action<ActorProviderCatalog.Option> add)
        {
            var window = CreateInstance<ActorProviderPicker>();
            window._data = data;
            window._contains = contains;
            window._add = add;
            window._options = ActorProviderCatalog.GetOptions(data);
            window.titleContent = new GUIContent(data ? "Add data" : "Add behaviour");
            window.minSize = new Vector2(360f, 340f);
            window.position = new Rect(250f, 180f, 490f, 480f);
            window.ShowUtility();
        }

        public void CreateGUI()
        {
            var root = ActorEditorStyles.Root(rootVisualElement);
            root.Add(ActorEditorStyles.Header(_data ? "Add data" : "Add behaviour", "Search module names and namespaces."));
            _search = new ToolbarSearchField();
            _search.AddToClassList("abc-search");
            _search.style.flexGrow = 0;
            _search.RegisterValueChangedCallback(evt => Filter(evt.newValue));
            root.Add(_search);
            _list = new ListView
            {
                itemsSource = _filtered,
                fixedItemHeight = 58,
                makeItem = MakeRow,
                bindItem = BindRow,
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight
            };
            _list.AddToClassList("abc-list");
            _list.selectionChanged += _ => RefreshSelection();
            _list.itemsChosen += _ => Confirm();
            root.Add(_list);
            _status = ActorEditorStyles.Text("", "abc-empty");
            root.Add(_status);
            _confirm = ActorEditorStyles.Button("Add module", Confirm, true);
            root.Add(_confirm);
            Filter(string.Empty);
            root.schedule.Execute(() => _search.Focus());
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
            var option = _filtered[index];
            var exists = _contains == null || _contains(option.ModuleType);
            ((Label)row[0]).text = option.ModuleType.Name + (exists ? "  /  Already added" : string.Empty);
            ((Label)row[1]).text = option.ModuleType.Namespace ?? "Global namespace";
            row.tooltip = option.ProviderType.FullName;
            row.style.opacity = exists ? 0.5f : 1f;
        }

        private void Filter(string query)
        {
            _filtered.Clear();
            if (_options != null)
            {
                for (var i = 0; i < _options.Count; i++)
                {
                    var option = _options[i];
                    if ((option.ModuleType.FullName ?? option.ModuleType.Name).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                        _filtered.Add(option);
                }
            }
            _list.RefreshItems();
            _list.selectedIndex = _filtered.Count > 0 ? 0 : -1;
            _status.text = _filtered.Count == 0 ? "No matching providers. Generate one with Feature Scaffold." : $"{_filtered.Count} available module type(s)";
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            var option = _list.selectedItem as ActorProviderCatalog.Option;
            _confirm.SetEnabled(option != null && _contains != null && !_contains(option.ModuleType));
        }

        private void Confirm()
        {
            var option = _list.selectedItem as ActorProviderCatalog.Option;
            if (option == null || _contains == null || _contains(option.ModuleType))
                return;
            _add?.Invoke(option);
            Close();
        }
    }
}
