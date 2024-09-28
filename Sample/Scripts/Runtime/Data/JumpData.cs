using abc.unity.Core;
using UnityEngine;

namespace abc.unity.Example
{
    public class JumpData : MonoBehaviour, IActorData
    {
       [field: SerializeField] public float Force { get; private set; }
    }
}