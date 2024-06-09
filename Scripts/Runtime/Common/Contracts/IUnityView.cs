using UnityEngine;

namespace abc.unity.Common
{
    public interface IUnityView
    {
        Transform transform { get; }
        GameObject gameObject { get; }
    }
}