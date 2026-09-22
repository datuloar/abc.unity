using System;

using UnityEngine;

namespace Abc.Unity.Samples.Basic
{
    [Serializable]
    public sealed class RotationSpeedComponent : IRotationSpeedComponent
    {
        [field: SerializeField] public float Value { get; private set; }
    }
}
