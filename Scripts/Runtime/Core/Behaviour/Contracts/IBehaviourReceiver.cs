﻿namespace abc.unity.Core
{
    public interface IBehaviourReceiver
    {
        void AddBehaviour<TBehaviour>(TBehaviour behaviour) where TBehaviour : IBehaviour;
    }
}