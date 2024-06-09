using System;
using UnityEngine;

namespace abc.unity.Core
{
    public abstract class ActorDataProviderBase : ScriptableObject
    {
        public abstract IActorData GetData();

        public abstract Type GetDataType();
    }
}