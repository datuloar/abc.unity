using UnityEditor;

namespace Abc.Unity.Editor
{
    [CustomEditor(typeof(ActorBehaviourProviderBase), true)]
    [CanEditMultipleObjects]
    internal sealed class ActorBehaviourProviderEditor : ActorProviderEditorBase
    {
    }
}
