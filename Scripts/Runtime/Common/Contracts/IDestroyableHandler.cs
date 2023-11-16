using System;

namespace abc.unity.Common
{
    public interface IDestroyableHandler<TArg>
    {
        public event Action<TArg> Destroyed;

        void Destroy();
    }
}