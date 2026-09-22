using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using Abc.Unity.Editor;

namespace Abc.Unity.Tests
{
    public sealed class ActorEditorToolkitTests
    {
        private sealed class ObservableData : IActorData
        {
            public int Value { get; set; } = 42;
            public int UnsafeProperty => throw new System.InvalidOperationException();
        }

        [Test]
        public void RuntimeValueViewRefreshesExistingElementsWithoutInvokingGetters()
        {
            var data = new ObservableData();
            var root = new VisualElement();
            ActorWorldExplorerFields.Populate(root, data, false);
            var field = root.Q<TextField>();
            Assert.That(field.value, Is.EqualTo("42"));
            Assert.That(field.isReadOnly, Is.True);
            Assert.That(root.childCount, Is.EqualTo(1));

            data.Value = 99;
            ActorWorldExplorerFields.Refresh(root, data);

            Assert.That(root.Q<TextField>(), Is.SameAs(field));
            Assert.That(field.value, Is.EqualTo("99"));
        }

        [Test]
        public void SharedThemeLoadsThroughItsStableAssetGuid()
        {
            var root = ActorEditorStyles.Root();
            Assert.That(root.styleSheets.count, Is.EqualTo(1));
            Assert.That(root.ClassListContains(EditorGUIUtility.isProSkin ? "abc-dark" : "abc-light"), Is.True);
        }

        [Test]
        public void ExplorerUsesVirtualizedKeyboardNavigableResults()
        {
            var window = ScriptableObject.CreateInstance<ActorWorldExplorerWindow>();
            try
            {
                window.CreateGUI();
                var list = window.rootVisualElement.Q<ListView>("ActorResults");
                Assert.That(list, Is.Not.Null);
                Assert.That(list.virtualizationMethod, Is.EqualTo(CollectionVirtualizationMethod.FixedHeight));
                Assert.That(list.selectionType, Is.EqualTo(SelectionType.Single));
                Assert.That(window.rootVisualElement.Query<IMGUIContainer>().ToList(), Is.Empty);
                Assert.That(window.rootVisualElement.Q<ToolbarSearchField>("WorldExplorerSearch"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void ActorInspectorExposesSerializedFieldsWithoutLegacyContainers()
        {
            var gameObject = new GameObject("Toolkit Actor");
            var actor = gameObject.AddComponent<Actor>();
            var editor = UnityEditor.Editor.CreateEditor(actor);
            try
            {
                var root = editor.CreateInspectorGUI();
                var fields = root.Query<PropertyField>().ToList();
                Assert.That(fields.Exists(field => field.bindingPath == "_tag._value"), Is.True);
                Assert.That(fields.Exists(field => field.bindingPath == "_blueprints"), Is.True);
                Assert.That(fields.Exists(field => field.bindingPath == "_hasFixedUpdate"), Is.True);
                Assert.That(root.Query<IMGUIContainer>().ToList(), Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TagPresetSupportsMultiObjectEditingAndUndo()
        {
            var first = new GameObject("First");
            var second = new GameObject("Second");
            var actors = new Object[] { first.AddComponent<Actor>(), second.AddComponent<Actor>() };
            try
            {
                using var serialized = new SerializedObject(actors);
                Undo.IncrementCurrentGroup();
                ActorTagDrawer.ApplyPreset(serialized.FindProperty("_tag._value._value"), 2);
                Undo.FlushUndoRecordObjects();

                foreach (var actor in actors)
                    Assert.That(((Actor)actor).Tag.Value, Is.EqualTo(ActorTag.Enemy));

                Undo.PerformUndo();
                foreach (var actor in actors)
                    Assert.That(((Actor)actor).Tag.Value, Is.EqualTo(ActorTag.Default));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void ProviderInspectorUsesNativeSerializedFields()
        {
            var provider = ScriptableObject.CreateInstance<SerializedDataProvider>();
            var editor = UnityEditor.Editor.CreateEditor(provider);
            try
            {
                var root = editor.CreateInspectorGUI();
                Assert.That(root, Is.Not.Null);
                Assert.That(root.Q<PropertyField>(), Is.Not.Null);
                Assert.That(root.Query<IMGUIContainer>().ToList(), Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(provider);
            }
        }

        [Test]
        public void ScaffoldAndDashboardBuildRetainedVisualTrees()
        {
            var scaffold = ScriptableObject.CreateInstance<ActorFeatureWizard>();
            var dashboard = ScriptableObject.CreateInstance<ActorDashboardWindow>();
            try
            {
                scaffold.CreateGUI();
                dashboard.CreateGUI();
                Assert.That(scaffold.rootVisualElement.Query<IMGUIContainer>().ToList(), Is.Empty);
                Assert.That(dashboard.rootVisualElement.Query<IMGUIContainer>().ToList(), Is.Empty);
                Assert.That(scaffold.rootVisualElement.Q<PopupField<string>>(), Is.Not.Null);
                Assert.That(scaffold.rootVisualElement.Query<TextField>().ToList().Exists(field => field.isReadOnly), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(scaffold);
                Object.DestroyImmediate(dashboard);
            }
        }
    }
}
