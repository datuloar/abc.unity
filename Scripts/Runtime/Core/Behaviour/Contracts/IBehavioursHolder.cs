﻿namespace abc.unity.Core
{
    public interface IBehavioursHolder
    {
        void AddBehaviour<TBehaviour>(TBehaviour behaviour) where TBehaviour : IBehaviour;
        bool HasBehaviour<TBehaviour>() where TBehaviour : IBehaviour;
        void RemoveBehaviour<TBehaviour>() where TBehaviour : IBehaviour;
    }
}