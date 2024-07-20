using UnityEngine;

namespace abc.unity.Example
{
    public class JumpData : MonoBehaviour, IJumpData
    {
       [field: SerializeField] public float Force { get; private set; }
    }
}