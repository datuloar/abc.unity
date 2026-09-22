using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.Basic
{
    public sealed class JumpData : MonoBehaviour, IActorData
    {
       [field: SerializeField] public float Force { get; private set; }
    }
}
