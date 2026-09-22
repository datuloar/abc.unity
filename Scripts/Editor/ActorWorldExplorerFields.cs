using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Abc.Unity;

namespace Abc.Unity.Editor
{
    internal static class ActorWorldExplorerFields
    {
        private const int MaxVisibleFields = 64;
        private static readonly Dictionary<Type, FieldInfo[]> FieldsByType = new Dictionary<Type, FieldInfo[]>();
        private static readonly Dictionary<Type, FieldInfo[]> AllFieldsByType = new Dictionary<Type, FieldInfo[]>();

        internal static void Populate(VisualElement container, IActorModule module, bool includePrivate)
        {
            container.Clear();
            if (module == null)
                return;
            var fields = GetFields(module.GetType(), includePrivate);
            if (fields.Length == 0)
            {
                container.Add(ActorEditorStyles.Text("No visible fields", "abc-muted"));
                return;
            }
            var visible = Mathf.Min(fields.Length, MaxVisibleFields);
            for (var i = 0; i < visible; i++)
            {
                var field = fields[i];
                var view = ActorEditorStyles.ReadOnly(GetLabel(field));
                view.userData = field;
                view.tooltip = field.FieldType.FullName;
                container.Add(view);
            }
            if (fields.Length > visible)
                container.Add(ActorEditorStyles.Text($"{fields.Length - visible} more fields", "abc-muted"));
            Refresh(container, module);
        }

        internal static void Refresh(VisualElement container, IActorModule module)
        {
            if (container == null || module == null)
                return;
            foreach (var child in container.Children())
            {
                if (child is TextField text && child.userData is FieldInfo field)
                    text.SetValueWithoutNotify(FormatValue(ReadField(module, field)));
            }
        }

        internal static FieldInfo[] GetFields(Type type, bool includePrivate)
        {
            var cache = includePrivate ? AllFieldsByType : FieldsByType;
            if (cache.TryGetValue(type, out var cached))
                return cached;

            var visible = new List<FieldInfo>();
            for (var current = type; current != null && current != typeof(object) &&
                 current != typeof(MonoBehaviour) && current != typeof(ScriptableObject); current = current.BaseType)
            {
                var fields = current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (var i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                        continue;
                    if (includePrivate || IsVisible(field))
                        visible.Add(field);
                }
            }

            cached = visible.ToArray();
            cache.Add(type, cached);
            return cached;
        }

        private static bool IsVisible(FieldInfo field)
        {
            if (Attribute.IsDefined(field, typeof(HideInInspector)) || field.IsNotSerialized)
                return false;
            if (field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField)) ||
                Attribute.IsDefined(field, typeof(SerializeReference)))
                return true;
            if (!Attribute.IsDefined(field, typeof(CompilerGeneratedAttribute)) || !field.Name.StartsWith("<", StringComparison.Ordinal))
                return false;

            var end = field.Name.IndexOf('>');
            return end > 1 && field.DeclaringType.GetProperty(field.Name.Substring(1, end - 1),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) != null;
        }

        private static object ReadField(object target, FieldInfo field)
        {
            try { return field.GetValue(target); }
            catch (Exception) { return "Unavailable"; }
        }

        private static string GetLabel(FieldInfo field)
        {
            var name = field.Name;
            if (name.StartsWith("<", StringComparison.Ordinal) && name.IndexOf('>') > 1)
                name = name.Substring(1, name.IndexOf('>') - 1);
            return ObjectNames.NicifyVariableName(name);
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return "null";
            if (value is UnityEngine.Object unityObject)
                return unityObject != null ? unityObject.name : "Destroyed";
            if (value is Array array)
                return $"{array.GetType().GetElementType()?.Name}[{array.Length}]";
            if (value is IList list && value.GetType().Assembly == typeof(List<>).Assembly)
                return $"{value.GetType().Name} ({list.Count} items)";
            var type = value.GetType();
            return type.IsPrimitive || type.IsEnum || value is string || value is decimal ||
                   value is Vector2 || value is Vector3 || value is Vector4 || value is Quaternion || value is Color
                ? value.ToString()
                : type.Name;
        }

        internal static void AppendSnapshot(StringBuilder builder, IActorModule module, bool includePrivate)
        {
            var fields = GetFields(module.GetType(), includePrivate);
            var count = Math.Min(fields.Length, MaxVisibleFields);
            for (var i = 0; i < count; i++)
            {
                var field = fields[i];
                builder.Append("  ").Append(GetLabel(field)).Append(": ")
                    .AppendLine(FormatValue(ReadField(module, field)));
            }
            if (fields.Length > count)
                builder.Append("  ").Append(fields.Length - count).AppendLine(" more fields");
        }
    }
}
