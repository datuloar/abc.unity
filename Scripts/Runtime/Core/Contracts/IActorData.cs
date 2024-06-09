namespace abc.unity.Core
{
    public interface IActorData : IActorModule 
    {
        void PreInitialize() { }
        void Initialize() { }

        void CleanUp() { }
    }
}