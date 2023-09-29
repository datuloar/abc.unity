using abc.unity.Common;
using abc.unity.Core;
using UnityEngine;

namespace abc.unity.Example
{
    public class RotationBehaviour : IRotationBehaviour
    {
        public IActor Actor { get; set; }

        public void Tick(float deltaTime)
        {
            Debug.Log("Rotation");
        }

        public void Dispose() { }
    }

    public interface IRotationBehaviour : IBehaviour, ITickable { }
}