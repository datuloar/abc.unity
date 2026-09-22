using System;

using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.Basic
{
    [Serializable]
    public sealed class UpScaleBehaviour : IActorBehaviour, IActorTick, ICloneable
    {
        private Transform _transform;
        private UpScaleComponent _settings;

        public IActor Owner { get; set; }

        public void Initialize()
        {
            if (Owner is not IActorView view)
                throw new InvalidOperationException($"{nameof(UpScaleBehaviour)} requires a Unity actor view.");

            _transform = view.transform;
            _settings = Owner.GetData<UpScaleComponent>();
        }

        public void Tick(float deltaTime)
        {
            var target = Vector3.one * _settings.MaxScale;
            _transform.localScale = Vector3.MoveTowards(_transform.localScale, target, _settings.Increment * deltaTime);
        }

        public object Clone() => new UpScaleBehaviour();
    }
}
