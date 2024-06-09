namespace abc.unity.Core
{
    public interface IActorBehaviour : IActorModule
    {
        IActor Owner { get; set; }

        void PreInitialize() { }
        void Initialize() { }

        void CleanUp() { }
    }
}