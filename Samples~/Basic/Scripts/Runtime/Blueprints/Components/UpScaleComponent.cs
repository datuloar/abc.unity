using System;

using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.Basic
{
    [Serializable]
    public sealed class UpScaleComponent : IActorData, ICloneable
    {
        [SerializeField] private float _maxScale = 200f;
        [SerializeField] private float _increment = 2f;

        public float MaxScale => _maxScale;
        public float Increment => _increment;

        public UpScaleComponent()
        {
        }

        public UpScaleComponent(float maxScale, float increment)
        {
            _maxScale = maxScale;
            _increment = increment;
        }

        public object Clone() => new UpScaleComponent(_maxScale, _increment);
    }
}
