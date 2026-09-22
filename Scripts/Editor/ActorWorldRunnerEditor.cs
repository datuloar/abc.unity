using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Abc.Unity.Editor
{
    [CustomEditor(typeof(ActorWorldRunner))]
    internal sealed class ActorWorldRunnerEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = ActorEditorStyles.Root();
            root.Add(ActorEditorStyles.Header("World Runner", "A Unity lifecycle host for scene-free simulation."));
            var identity = ActorEditorStyles.Card("World");
            identity.Add(ActorEditorStyles.Property(serializedObject, "_worldName", "Name"));
            identity.Add(ActorEditorStyles.Property(serializedObject, "_initialCapacity", "Initial capacity"));
            root.Add(identity);
            root.Add(ActorEditorStyles.Property(serializedObject, "_automaticUpdates", "Automatic updates"));
            var clockHint = new HelpBox(
                "Disable automatic updates when a network clock drives this world. Only one clock may advance simulation and physics.",
                HelpBoxMessageType.Info);
            root.Add(clockHint);
            var phases = ActorEditorStyles.Card("Automatic phases");
            phases.Add(ActorEditorStyles.Property(serializedObject, "_runUpdate", "Update"));
            phases.Add(ActorEditorStyles.Property(serializedObject, "_runFixedUpdate", "Fixed Update"));
            phases.Add(ActorEditorStyles.Property(serializedObject, "_runLateUpdate", "Late Update"));
            root.Add(phases);
            root.TrackSerializedObjectValue(serializedObject, _ => RefreshClock());
            void RefreshClock()
            {
                var automatic = serializedObject.FindProperty("_automaticUpdates").boolValue;
                phases.SetEnabled(automatic);
                clockHint.style.display = automatic ? DisplayStyle.None : DisplayStyle.Flex;
            }
            RefreshClock();
            AddRuntime(root, identity);
            return root;
        }

        private void AddRuntime(VisualElement root, VisualElement identity)
        {
            var runner = (ActorWorldRunner)target;
            var runtime = ActorEditorStyles.Card("Simulation");
            var message = ActorEditorStyles.Text("The world is created in Awake or through GetOrCreateWorld().", "abc-muted");
            runtime.Add(message);
            var metrics = ActorEditorStyles.Row("abc-metrics");
            var count = ActorEditorStyles.Metric(metrics, "Models");
            var capacity = ActorEditorStyles.Metric(metrics, "Capacity");
            var indexes = ActorEditorStyles.Metric(metrics, "Indexes");
            runtime.Add(metrics);
            var actions = ActorEditorStyles.Row();
            var create = ActorEditorStyles.Button("Create world", () => runner.GetOrCreateWorld());
            var dispose = ActorEditorStyles.Button("Dispose world", runner.DisposeWorld);
            var explore = ActorEditorStyles.Button("Explore world", () => ActorWorldExplorerWindow.OpenWorld(runner.World), true);
            actions.Add(create);
            actions.Add(explore);
            actions.Add(dispose);
            runtime.Add(actions);
            root.Add(runtime);
            void Refresh()
            {
                if (runner == null)
                    return;
                identity.SetEnabled(!EditorApplication.isPlaying);
                var world = runner.HasWorld ? runner.World : null;
                count.text = world?.Count.ToString() ?? "—";
                capacity.text = world?.Capacity.ToString() ?? "—";
                indexes.text = world?.IndexedDataTypeCount.ToString() ?? "—";
                create.style.display = world == null ? DisplayStyle.Flex : DisplayStyle.None;
                create.SetEnabled(EditorApplication.isPlaying);
                explore.SetEnabled(world != null);
                dispose.SetEnabled(world != null && EditorApplication.isPlaying);
                message.style.display = world == null ? DisplayStyle.Flex : DisplayStyle.None;
            }
            runtime.schedule.Execute(Refresh).Every(500);
            Refresh();
        }
    }
}
