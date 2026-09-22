using UnityEditor;
using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Editor
{
    [CustomEditor(typeof(ActorWorldRunner))]
    internal sealed class ActorWorldRunnerEditor : UnityEditor.Editor
    {
        private SerializedProperty _worldName;
        private SerializedProperty _initialCapacity;
        private SerializedProperty _runUpdate;
        private SerializedProperty _runFixedUpdate;
        private SerializedProperty _runLateUpdate;

        private void OnEnable()
        {
            _worldName = serializedObject.FindProperty("_worldName");
            _initialCapacity = serializedObject.FindProperty("_initialCapacity");
            _runUpdate = serializedObject.FindProperty("_runUpdate");
            _runFixedUpdate = serializedObject.FindProperty("_runFixedUpdate");
            _runLateUpdate = serializedObject.FindProperty("_runLateUpdate");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ActorEditorStyles.DrawHeader("Actor World Runner", "Unity lifecycle host for scene-free simulation", "d_UnityEditor.GameView");

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                ActorEditorStyles.BeginCard();
                GUILayout.Label("World", ActorEditorStyles.Section);
                EditorGUILayout.PropertyField(_worldName, new GUIContent("Name"));
                EditorGUILayout.PropertyField(_initialCapacity, new GUIContent("Initial Capacity"));
                ActorEditorStyles.EndCard();
            }

            ActorEditorStyles.BeginCard();
            GUILayout.Label("Automatic Phases", ActorEditorStyles.Section);
            EditorGUILayout.BeginHorizontal();
            _runUpdate.boolValue = GUILayout.Toggle(_runUpdate.boolValue, "Update", EditorStyles.miniButtonLeft);
            _runFixedUpdate.boolValue = GUILayout.Toggle(_runFixedUpdate.boolValue, "Fixed", EditorStyles.miniButtonMid);
            _runLateUpdate.boolValue = GUILayout.Toggle(_runLateUpdate.boolValue, "Late", EditorStyles.miniButtonRight);
            EditorGUILayout.EndHorizontal();
            ActorEditorStyles.EndCard();

            serializedObject.ApplyModifiedProperties();
            DrawRuntime((ActorWorldRunner)target);
        }

        private static void DrawRuntime(ActorWorldRunner runner)
        {
            ActorEditorStyles.BeginCard();
            GUILayout.Label("Runtime", ActorEditorStyles.Section);

            if (!runner.HasWorld)
            {
                EditorGUILayout.HelpBox("The world is created in Awake or explicitly through GetOrCreateWorld().", MessageType.Info);

                if (EditorApplication.isPlaying && GUILayout.Button("Create World", ActorEditorStyles.CenteredButton))
                    runner.GetOrCreateWorld();

                ActorEditorStyles.EndCard();
                return;
            }

            var world = runner.World;
            EditorGUILayout.BeginHorizontal();
            ActorEditorStyles.DrawMetric(world.Count.ToString(), "Models");
            ActorEditorStyles.DrawMetric(world.Capacity.ToString(), "Capacity");
            ActorEditorStyles.DrawMetric(world.IndexedDataTypeCount.ToString(), "Query Indexes");
            EditorGUILayout.EndHorizontal();

            if (EditorApplication.isPlaying && GUILayout.Button("Dispose World", ActorEditorStyles.CenteredButton))
                runner.DisposeWorld();

            ActorEditorStyles.EndCard();
        }
    }
}
