using abc.unity.Core;

namespace abc.unity.Example
{
    public class Hero : Actor, IHero { }

    public interface IHero : IActor { }
}