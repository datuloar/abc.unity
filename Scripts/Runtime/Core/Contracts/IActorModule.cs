namespace Abc.Unity
{
    public interface IActorModule
    {
        void PreInitialize() { }
        void Initialize() { }

        void CleanUp() { }
    }
}
