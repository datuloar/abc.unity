using System;

namespace Abc.Unity
{
    public sealed partial class ActorWorld
    {
        private void ClearCore()
        {
            if (_clearing)
                return;

            _clearing = true;

            try
            {
                ClearDataIndexes();
                DetachAll(_actors, _slotCount);
                DetachAll(_pending, _pendingCount);
                _liveCount = 0;

                for (var i = _slotCount - 1; i >= 0; i--)
                    _actors[i]?.Destroy();

                for (var i = _pendingCount - 1; i >= 0; i--)
                    _pending[i]?.Destroy();
            }
            finally
            {
                Array.Clear(_actors, 0, _slotCount);
                Array.Clear(_pending, 0, _pendingCount);
                _slotCount = 0;
                _pendingCount = 0;
                _liveCount = 0;
                _requiresCompaction = false;
                _clearing = false;
            }
        }

        private static void DetachAll(ActorModel[] actors, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var actor = actors[i];
                if (actor == null)
                    continue;

                actor.World = null;
                actor.WorldIndex = -1;
                actor.IsPendingInWorld = false;
                actor.IsActiveInWorld = false;
            }
        }
    }
}
