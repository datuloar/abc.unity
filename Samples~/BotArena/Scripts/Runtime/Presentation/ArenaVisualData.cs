using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    internal sealed class ArenaVisualData : IActorData
    {
        private const float HealthBarWidth = 1.35f;

        private readonly Transform _healthFill;

        public ArenaVisualData(Transform root, Transform healthFill, Color accent)
        {
            Root = root;
            _healthFill = healthFill;
            Accent = accent;
            Heading = Vector3.forward;
        }

        public Transform Root { get; }
        public Color Accent { get; }
        public Vector3 Heading { get; set; }

        public void Sync(Vector3 position, Vector3 velocity)
        {
            if (Root == null)
                return;

            Root.position = position;
            var forward = Heading.sqrMagnitude > 0.001f ? Heading : velocity;
            if (forward.sqrMagnitude > 0.001f)
                Root.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        public void SetHealth(float normalized)
        {
            if (_healthFill == null)
                return;

            normalized = Mathf.Clamp01(normalized);
            var scale = _healthFill.localScale;
            scale.x = Mathf.Max(0.001f, HealthBarWidth * normalized);
            _healthFill.localScale = scale;
            var position = _healthFill.localPosition;
            position.x = (scale.x - HealthBarWidth) * 0.5f;
            _healthFill.localPosition = position;
        }

        public void CleanUp()
        {
            if (Root != null)
                Object.Destroy(Root.gameObject);
        }
    }
}
