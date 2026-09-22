using UnityEngine;

namespace Abc.Unity.Samples.BotArena
{
    internal static class ArenaHitTest
    {
        public static bool TryHit(Vector3 relativeStart, Vector3 relativeEnd, float radius, out float fraction)
        {
            fraction = 0f;
            var distanceSquared = relativeStart.sqrMagnitude - radius * radius;
            if (distanceSquared <= 0f)
                return true;

            var motion = relativeEnd - relativeStart;
            var lengthSquared = motion.sqrMagnitude;
            var approach = Vector3.Dot(relativeStart, motion);
            if (lengthSquared <= Mathf.Epsilon || approach >= 0f)
                return false;

            var discriminant = approach * approach - lengthSquared * distanceSquared;
            if (discriminant < 0f)
                return false;

            fraction = distanceSquared / (-approach + Mathf.Sqrt(discriminant));
            return fraction <= 1f;
        }
    }
}
