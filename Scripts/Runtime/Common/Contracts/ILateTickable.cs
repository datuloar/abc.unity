namespace abc.unity.Common
{
    public interface ILateTickable
    {
        void LateTick(float deltaTime);
    }
}