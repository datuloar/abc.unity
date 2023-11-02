namespace abc.unity.Common
{
    public interface IDeadable
    {
        bool IsAlive { get; }

        void SetDead(bool isDead);
    }
}