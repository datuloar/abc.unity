using UnityEditor;
using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Editor
{
    [CustomPropertyDrawer(typeof(ActorTag))]
    internal sealed class ActorTagDrawer : PropertyDrawer
    {
        private const int FirstCustomId = 100;

        private static readonly GUIContent[] Options =
        {
            new GUIContent("Default"),
            new GUIContent("Player"),
            new GUIContent("Enemy"),
            new GUIContent("Custom")
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var value = property.FindPropertyRelative("_value");
            if (value == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label);

            if (position.width < 150f)
            {
                DrawIdField(position, value);
                EditorGUI.EndProperty();
                return;
            }

            var spacing = EditorGUIUtility.standardVerticalSpacing;
            var popupWidth = Mathf.Min(120f, position.width * 0.56f);
            var popupRect = new Rect(position.x, position.y, popupWidth, position.height);
            var idRect = new Rect(popupRect.xMax + spacing, position.y, position.width - popupWidth - spacing, position.height);
            var currentIndex = GetOptionIndex(value.intValue);
            var previousMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = value.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var selectedIndex = EditorGUI.Popup(popupRect, currentIndex, Options);

            if (EditorGUI.EndChangeCheck())
                value.intValue = selectedIndex < 3 ? selectedIndex : FirstCustomId;

            DrawIdField(idRect, value);
            EditorGUI.showMixedValue = previousMixedValue;
            EditorGUI.EndProperty();
        }

        private static int GetOptionIndex(int value) => value >= 0 && value < 3 ? value : 3;

        private static void DrawIdField(Rect position, SerializedProperty value)
        {
            EditorGUI.BeginChangeCheck();
            var id = EditorGUI.IntField(position, value.intValue);
            if (EditorGUI.EndChangeCheck())
                value.intValue = id;
        }
    }
}
