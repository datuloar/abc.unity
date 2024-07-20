using abc.unity.Core;
using System;

namespace abc.unity.Example
{
    [Serializable]
    public class UpScaleBehaviour : IActorBehaviour
    {
        public IActor Owner { get; set; }
    }
}