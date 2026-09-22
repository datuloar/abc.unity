using System;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Abc.Unity.Editor
{
    internal static class ActorEditorStyles
    {
        private const string StyleGuid = "f4d9380a7d014a1383cc8c416766d0fa";
        private static StyleSheet _sheet;

        internal static VisualElement Root(VisualElement root = null)
        {
            root ??= new VisualElement();
            root.Clear();
            root.AddToClassList("abc-root");
            root.EnableInClassList("abc-dark", EditorGUIUtility.isProSkin);
            root.EnableInClassList("abc-light", !EditorGUIUtility.isProSkin);
            root.schedule.Execute(() =>
            {
                root.EnableInClassList("abc-dark", EditorGUIUtility.isProSkin);
                root.EnableInClassList("abc-light", !EditorGUIUtility.isProSkin);
            }).Every(1000);
            if (_sheet == null)
                _sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(StyleGuid));
            if (_sheet != null && !root.styleSheets.Contains(_sheet))
                root.styleSheets.Add(_sheet);
            return root;
        }

        internal static VisualElement Header(string title, string subtitle)
        {
            var header = new VisualElement();
            header.AddToClassList("abc-header");
            header.Add(Text("ABC  /  WORKSPACE", "abc-eyebrow"));
            header.Add(Text(title, "abc-title"));
            header.Add(Text(subtitle, "abc-subtitle"));
            return header;
        }

        internal static Label Text(string text, string className = "abc-body")
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        internal static VisualElement Card(string title = null, string subtitle = null)
        {
            var card = new VisualElement();
            card.AddToClassList("abc-card");
            if (!string.IsNullOrEmpty(title))
                card.Add(Text(title, "abc-section"));
            if (!string.IsNullOrEmpty(subtitle))
                card.Add(Text(subtitle, "abc-muted"));
            return card;
        }

        internal static VisualElement Row(string className = null)
        {
            var row = new VisualElement();
            row.AddToClassList("abc-row");
            if (className != null)
                row.AddToClassList(className);
            return row;
        }

        internal static Button Button(string title, Action clicked, bool primary = false)
        {
            var button = new Button(clicked) { text = title };
            button.AddToClassList("abc-button");
            button.EnableInClassList("abc-primary", primary);
            return button;
        }

        internal static Label Metric(VisualElement parent, string title)
        {
            var metric = new VisualElement();
            metric.AddToClassList("abc-metric");
            var value = Text("0", "abc-metric-value");
            metric.Add(value);
            metric.Add(Text(title, "abc-muted"));
            parent.Add(metric);
            return value;
        }

        internal static PropertyField Property(SerializedObject serialized, string path, string label)
        {
            var field = new PropertyField(serialized.FindProperty(path), label);
            field.AddToClassList("abc-property");
            return field;
        }

        internal static TextField ReadOnly(string label, string value = "")
        {
            var field = new TextField(label) { value = value, isReadOnly = true };
            field.AddToClassList("abc-readonly");
            return field;
        }

        internal static ScrollView Scroll(VisualElement parent)
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("abc-scroll");
            parent.Add(scroll);
            return scroll;
        }
    }
}
