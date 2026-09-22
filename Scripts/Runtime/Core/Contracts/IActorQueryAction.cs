namespace Abc.Unity
{
    public interface IActorQueryAction<TData> where TData : class, IActorData
    {
        void Execute(ActorModel actor, TData data);
    }

    public interface IActorQueryAction<TData1, TData2>
        where TData1 : class, IActorData
        where TData2 : class, IActorData
    {
        void Execute(ActorModel actor, TData1 data1, TData2 data2);
    }

    public interface IActorQueryAction<TData1, TData2, TData3>
        where TData1 : class, IActorData
        where TData2 : class, IActorData
        where TData3 : class, IActorData
    {
        void Execute(ActorModel actor, TData1 data1, TData2 data2, TData3 data3);
    }
}
