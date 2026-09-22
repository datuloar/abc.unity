using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Abc.Unity.Editor
{
    [CustomPropertyDrawer(typeof(ActorTag))]
    internal sealed class ActorTagDrawer : PropertyDrawer
    {
        private const int FirstCustomId = 100;
        private static readonly List<string> Options = new List<string> { "Default", "Player", "Enemy", "Custom" };

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var value = property.FindPropertyRelative("_value");
            var root = new VisualElement();
            var preset = new PopupField<string>(property.displayName, Options, GetOptionIndex(value.intValue));
            preset.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            preset.tooltip = "Use a built-in tag or a stable explicit integer ID for your game.";
            var id = new IntegerField("ID") { bindingPath = value.propertyPath };
            id.AddToClassList(BaseField<int>.alignedFieldUssClassName);
            id.tooltip = "Stable serialized ID. Never derive this from a runtime string hash.";
            root.Add(preset);
            root.Add(id);
            void Refresh(SerializedProperty current)
            {
                preset.showMixedValue = current.hasMultipleDifferentValues;
                preset.SetValueWithoutNotify(Options[GetOptionIndex(current.intValue)]);
            }
            preset.RegisterValueChangedCallback(evt => ApplyPreset(value, Options.IndexOf(evt.newValue)));
            root.TrackPropertyValue(value, Refresh);
            Refresh(value);
            return root;
        }

        private static int GetOptionIndex(int value) => value >= 0 && value < 3 ? value : 3;

        internal static void ApplyPreset(SerializedProperty value, int index)
        {
            if (index < 0 || index >= Options.Count)
                return;
            value.serializedObject.Update();
            value.intValue = index < 3 ? index : FirstCustomId;
            value.serializedObject.ApplyModifiedProperties();
        }
    }
}
