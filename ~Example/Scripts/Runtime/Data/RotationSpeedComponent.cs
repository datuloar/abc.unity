using System;
using UnityEngine;

namespace abc.unity.Example
{
    [Serializable]
    public class RotationSpeedComponent : IRotationSpeedComponent
    {
        [field: SerializeField] public float Value { get; private set; }
    }
}