using abc.unity.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace abc.unity.Example
{
    [Serializable]
    public class UpScaleComponent : IComponent, ICloneable
    {
        [SerializeField] private float _maxScale = 200f;
        [SerializeField] private float _increment = 2f;

        public UpScaleComponent(float maxScale, float increment)
        {
            _maxScale = maxScale;
            _increment = increment;
        }

        public object Clone() => new UpScaleComponent(_maxScale, _increment);
    }
}