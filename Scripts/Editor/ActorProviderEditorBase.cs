using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Abc.Unity.Editor
{
    internal abstract class ActorProviderEditorBase : UnityEditor.Editor
    {
        public sealed override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var property = serializedObject.GetIterator();
            if (!property.NextVisible(true))
                return root;
            do
            {
                if (property.propertyPath == "m_Script")
                    continue;
                if (property.propertyPath == "_value")
                    property.isExpanded = true;
                var field = new PropertyField(property.Copy());
                field.AddToClassList("abc-property");
                root.Add(field);
            }
            while (property.NextVisible(false));
            return root;
        }
    }
}
