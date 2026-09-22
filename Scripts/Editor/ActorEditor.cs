using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Abc.Unity.Editor
{
    [CustomEditor(typeof(Actor))]
    [CanEditMultipleObjects]
    internal sealed class ActorEditor : UnityEditor.Editor
    {
        private readonly HashSet<Object> _seenBlueprints = new HashSet<Object>();

        public override VisualElement CreateInspectorGUI()
        {
            var root = ActorEditorStyles.Root();
            root.Add(ActorEditorStyles.Header("Actor", targets.Length > 1
                ? $"{targets.Length} actors selected" : "Scene-backed gameplay. Compose, then play."));
            var authoring = new VisualElement();
            var identity = ActorEditorStyles.Card("Identity");
            identity.Add(ActorEditorStyles.Property(serializedObject, "_tag._value", "Tag"));
            authoring.Add(identity);
            var composition = ActorEditorStyles.Card("Blueprints", "Reusable composition, applied in list order.");
            composition.Add(ActorEditorStyles.Property(serializedObject, "_blueprints", "Blueprints"));
            authoring.Add(composition);
            var phases = ActorEditorStyles.Card("Lifecycle & scheduling");
            phases.Add(ActorEditorStyles.Property(serializedObject, "_initializeOnAwake", "Initialize on Awake"));
            phases.Add(ActorEditorStyles.Property(serializedObject, "_hasUpdate", "Update"));
            phases.Add(ActorEditorStyles.Property(serializedObject, "_hasFixedUpdate", "Fixed Update"));
            phases.Add(ActorEditorStyles.Property(serializedObject, "_hasLateUpdate", "Late Update"));
            authoring.Add(phases);
            root.Add(authoring);
            var validation = new HelpBox("", HelpBoxMessageType.Error);
            root.Add(validation);
            var explicitInitialization = new HelpBox("Call Initialize() explicitly for actors without Initialize on Awake.", HelpBoxMessageType.Info);
            root.Add(explicitInitialization);
            void Refresh()
            {
                if (target == null)
                    return;
                authoring.SetEnabled(!EditorApplication.isPlaying);
                serializedObject.UpdateIfRequiredOrScript();
                var error = ValidateBlueprints();
                validation.text = error;
                validation.style.display = error.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                var awake = serializedObject.FindProperty("_initializeOnAwake");
                explicitInitialization.style.display = !EditorApplication.isPlaying &&
                    (!awake.boolValue || awake.hasMultipleDifferentValues) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            root.TrackSerializedObjectValue(serializedObject, _ => Refresh());
            root.schedule.Execute(Refresh).Every(500);
            Refresh();
            if (!serializedObject.isEditingMultipleObjects)
                AddRuntime(root, (Actor)target);
            return root;
        }

        private string ValidateBlueprints()
        {
            var missing = 0;
            var duplicates = 0;
            foreach (var actor in targets)
            {
                using var serialized = new SerializedObject(actor);
                var blueprints = serialized.FindProperty("_blueprints");
                _seenBlueprints.Clear();
                for (var i = 0; i < blueprints.arraySize; i++)
                {
                    var blueprint = blueprints.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (blueprint == null)
                        missing++;
                    else if (!_seenBlueprints.Add(blueprint))
                        duplicates++;
                }
            }
            return missing + duplicates == 0 ? string.Empty :
                $"Blueprint references: {missing} missing, {duplicates} duplicates. Remove or replace them before Play Mode.";
        }

        private static void AddRuntime(VisualElement root, Actor actor)
        {
            var runtime = ActorEditorStyles.Card("Runtime", "Read-only diagnostics. Runtime actions do not change scene authoring.");
            var metrics = ActorEditorStyles.Row("abc-metrics");
            var state = ActorEditorStyles.Metric(metrics, "Lifecycle");
            var data = ActorEditorStyles.Metric(metrics, "Data");
            var behaviours = ActorEditorStyles.Metric(metrics, "Behaviours");
            runtime.Add(metrics);
            var actions = ActorEditorStyles.Row();
            var action = ActorEditorStyles.Button("Initialize", () => ChangeLifecycle(actor));
            actions.Add(action);
            actions.Add(ActorEditorStyles.Button("Inspect composition", () => ActorWorldExplorerWindow.OpenActor(actor), true));
            runtime.Add(actions);
            root.Add(runtime);
            void Refresh()
            {
                if (actor == null)
                    return;
                state.text = actor.LifecycleState;
                data.text = actor.DataCount.ToString();
                behaviours.text = actor.BehaviourCount.ToString();
                action.text = !actor.IsInitialized.Value ? "Initialize" : actor.IsAlive.Value ? "Kill" : "Revive";
                action.SetEnabled(EditorApplication.isPlaying && actor.LifecycleState != "Disposed");
            }
            runtime.schedule.Execute(Refresh).Every(500);
            Refresh();
        }

        private static void ChangeLifecycle(Actor actor)
        {
            if (actor == null || !EditorApplication.isPlaying || actor.LifecycleState == "Disposed")
                return;
            if (!actor.IsInitialized.Value)
                actor.Initialize();
            else if (actor.IsAlive.Value)
                actor.Kill();
            else
                actor.Revive();
        }
    }
}
