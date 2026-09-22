namespace Abc.Unity
{
    public interface IActorBehaviour : IActorModule
    {
        IActor Owner { get; set; }
    }
}
