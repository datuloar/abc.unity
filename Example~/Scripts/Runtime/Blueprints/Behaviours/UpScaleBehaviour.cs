using abc.unity.Core;
using System;

namespace abc.unity.Example
{
    [Serializable]
    public class UpScaleBehaviour : IBehaviour
    {
        public IActor Actor { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }
}