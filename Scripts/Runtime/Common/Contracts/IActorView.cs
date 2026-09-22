using UnityEngine;

namespace Abc.Unity
{
    public interface IActorView
    {
        Transform transform { get; }
        GameObject gameObject { get; }
    }
}
