using UnityEditor;

namespace Abc.Unity.Editor
{
    [CustomEditor(typeof(ActorDataProviderBase), true)]
    [CanEditMultipleObjects]
    internal sealed class ActorDataProviderEditor : ActorProviderEditorBase
    {
    }
}
