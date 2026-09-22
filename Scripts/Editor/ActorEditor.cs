using System.Collections.Generic;

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Editor
{
    [CustomEditor(typeof(Actor))]
    [CanEditMultipleObjects]
    internal sealed class ActorEditor : UnityEditor.Editor
    {
        private readonly HashSet<Object> _seenBlueprints = new HashSet<Object>();
        private SerializedProperty _tag;
        private SerializedProperty _blueprints;
        private SerializedProperty _initializeOnAwake;
        private SerializedProperty _hasUpdate;
        private SerializedProperty _hasFixedUpdate;
        private SerializedProperty _hasLateUpdate;
        private ReorderableList _blueprintList;

        private void OnEnable()
        {
            _tag = serializedObject.FindProperty("_tag").FindPropertyRelative("_value");
            _blueprints = serializedObject.FindProperty("_blueprints");
            _initializeOnAwake = serializedObject.FindProperty("_initializeOnAwake");
            _hasUpdate = serializedObject.FindProperty("_hasUpdate");
            _hasFixedUpdate = serializedObject.FindProperty("_hasFixedUpdate");
            _hasLateUpdate = serializedObject.FindProperty("_hasLateUpdate");
            _blueprintList = new ReorderableList(serializedObject, _blueprints, true, true, true, true)
            {
                drawHeaderCallback = DrawBlueprintHeader,
                drawElementCallback = DrawBlueprintElement,
                elementHeight = EditorGUIUtility.singleLineHeight + 6f
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ActorEditorStyles.DrawHeader("ABC Actor", GetHeaderSubtitle(), "d_GameObject Icon");

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                DrawIdentity();
                DrawBlueprints();
                DrawExecution();
            }

            serializedObject.ApplyModifiedProperties();
            DrawValidation();

            if (!serializedObject.isEditingMultipleObjects)
                DrawRuntime((Actor)target);
        }

        private void DrawIdentity()
        {
            ActorEditorStyles.BeginCard();
            GUILayout.Label("Identity", ActorEditorStyles.Section);
            EditorGUILayout.PropertyField(_tag, new GUIContent("Tag"));
            ActorEditorStyles.EndCard();
        }

        private void DrawBlueprints()
        {
            ActorEditorStyles.BeginCard();
            _blueprintList.DoLayoutList();
            ActorEditorStyles.EndCard();
        }

        private void DrawExecution()
        {
            ActorEditorStyles.BeginCard();
            GUILayout.Label("Lifecycle & Update", ActorEditorStyles.Section);
            EditorGUILayout.PropertyField(_initializeOnAwake, new GUIContent("Initialize On Awake"));
            GUILayout.Space(2f);
            EditorGUILayout.LabelField("Scheduled Phases", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _hasUpdate.boolValue = GUILayout.Toggle(_hasUpdate.boolValue, "Update", EditorStyles.miniButtonLeft);
            _hasFixedUpdate.boolValue = GUILayout.Toggle(_hasFixedUpdate.boolValue, "Fixed", EditorStyles.miniButtonMid);
            _hasLateUpdate.boolValue = GUILayout.Toggle(_hasLateUpdate.boolValue, "Late", EditorStyles.miniButtonRight);
            EditorGUILayout.EndHorizontal();
            ActorEditorStyles.EndCard();
        }

        private void DrawValidation()
        {
            var missing = 0;
            var duplicates = 0;
            _seenBlueprints.Clear();

            for (var i = 0; i < _blueprints.arraySize; i++)
            {
                var blueprint = _blueprints.GetArrayElementAtIndex(i).objectReferenceValue;
                if (blueprint == null)
                    missing++;
                else if (!_seenBlueprints.Add(blueprint))
                    duplicates++;
            }

            if (missing > 0)
                EditorGUILayout.HelpBox($"Remove {missing} missing blueprint reference(s).", MessageType.Error);

            if (duplicates > 0)
                EditorGUILayout.HelpBox($"Remove {duplicates} duplicate blueprint reference(s).", MessageType.Error);

            if (!_initializeOnAwake.boolValue && !EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("This actor requires an explicit Initialize() call.", MessageType.Info);
        }

        private void DrawRuntime(Actor actor)
        {
            ActorEditorStyles.BeginCard();
            GUILayout.Label(EditorApplication.isPlaying ? "Runtime" : "Runtime Preview", ActorEditorStyles.Section);
            EditorGUILayout.BeginHorizontal();
            ActorEditorStyles.DrawMetric(actor.LifecycleState, "State");
            ActorEditorStyles.DrawMetric(actor.DataCount.ToString(), "Data");
            ActorEditorStyles.DrawMetric(actor.BehaviourCount.ToString(), "Behaviours");
            EditorGUILayout.EndHorizontal();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to inspect live lifecycle state and modules.", MessageType.None);
                ActorEditorStyles.EndCard();
                return;
            }

            EditorGUILayout.BeginHorizontal();

            if (!actor.IsInitialized.Value)
            {
                if (GUILayout.Button("Initialize", ActorEditorStyles.CenteredButton))
                    actor.Initialize();
            }
            else if (actor.IsAlive.Value)
            {
                if (GUILayout.Button("Kill", ActorEditorStyles.CenteredButton))
                    actor.Kill();
            }
            else if (GUILayout.Button("Revive", ActorEditorStyles.CenteredButton))
            {
                actor.Revive();
            }

            EditorGUILayout.EndHorizontal();
            ActorEditorStyles.EndCard();
        }

        private void DrawBlueprintHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, $"Blueprints  {_blueprints.arraySize}", EditorStyles.boldLabel);
        }

        private void DrawBlueprintElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.y += 3f;
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, _blueprints.GetArrayElementAtIndex(index), GUIContent.none);
        }

        private string GetHeaderSubtitle()
        {
            if (serializedObject.isEditingMultipleObjects)
                return $"{targets.Length} actors selected";

            var actor = (Actor)target;
            return EditorApplication.isPlaying ? actor.LifecycleState : "Composable gameplay entity";
        }
    }
}
