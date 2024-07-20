namespace abc.unity.Core
{
    public interface IActorModule 
    {
        void PreInitialize() { }
        void Initialize() { }

        void CleanUp() { }
    }
}
