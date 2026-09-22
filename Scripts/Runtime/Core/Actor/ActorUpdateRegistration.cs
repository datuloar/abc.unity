
namespace Abc.Unity
{
    internal sealed class ActorUpdateRegistration
    {
        private bool _tickRegistered;
        private bool _fixedTickRegistered;
        private bool _lateTickRegistered;

        public void Refresh(Actor actor, bool tick, bool fixedTick, bool lateTick)
        {
            RefreshTick(actor, tick);
            RefreshFixedTick(actor, fixedTick);
            RefreshLateTick(actor, lateTick);
        }

        public void RemoveAll(Actor actor)
        {
            RefreshTick(actor, false);
            RefreshFixedTick(actor, false);
            RefreshLateTick(actor, false);
        }

        private void RefreshTick(Actor actor, bool shouldRegister)
        {
            if (shouldRegister == _tickRegistered)
                return;

            if (shouldRegister)
                ActorUpdateScheduler.AddTickable(actor);
            else
                ActorUpdateScheduler.RemoveTickable(actor);

            _tickRegistered = shouldRegister;
        }

        private void RefreshFixedTick(Actor actor, bool shouldRegister)
        {
            if (shouldRegister == _fixedTickRegistered)
                return;

            if (shouldRegister)
                ActorUpdateScheduler.AddFixedTickable(actor);
            else
                ActorUpdateScheduler.RemoveFixedTickable(actor);

            _fixedTickRegistered = shouldRegister;
        }

        private void RefreshLateTick(Actor actor, bool shouldRegister)
        {
            if (shouldRegister == _lateTickRegistered)
                return;

            if (shouldRegister)
                ActorUpdateScheduler.AddLateTickable(actor);
            else
                ActorUpdateScheduler.RemoveLateTickable(actor);

            _lateTickRegistered = shouldRegister;
        }
    }
}
