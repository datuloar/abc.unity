using System;

using UnityEngine;

namespace Abc.Unity
{
    public abstract class ActorDataProviderBase : ScriptableObject
    {
        public abstract IActorData GetData();

        public abstract Type GetDataType();
    }
}
