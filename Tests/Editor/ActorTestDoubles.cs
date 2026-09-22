using System;

using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Tests
{
    public interface ITestDataView : IActorData
    {
    }

    public sealed class TrackingData : ITestDataView
    {
        public int PreInitializeCount { get; private set; }
        public int InitializeCount { get; private set; }
        public int CleanUpCount { get; private set; }

        public void PreInitialize() => PreInitializeCount++;
        public void Initialize() => InitializeCount++;
        public void CleanUp() => CleanUpCount++;
    }

    public sealed class SecondaryData : ITestDataView
    {
    }

    public sealed class TertiaryData : IActorData
    {
    }

    public sealed class IndexedData<TMarker> : IActorData where TMarker : struct
    {
        public IndexedData(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    public readonly struct MarkerOne
    {
    }

    public readonly struct MarkerTwo
    {
    }

    public readonly struct MarkerThree
    {
    }

    public readonly struct MarkerFour
    {
    }

    public readonly struct MarkerFive
    {
    }

    public readonly struct MarkerSix
    {
    }

    public readonly struct MarkerSeven
    {
    }

    public readonly struct MarkerEight
    {
    }

    public readonly struct MarkerNine
    {
    }

    public readonly struct MarkerTen
    {
    }

    public sealed class TestComponentData : MonoBehaviour, IActorData
    {
    }

    public readonly struct TestCommand : IActorCommand
    {
    }

    public readonly struct AlternateTestCommand : IActorCommand
    {
    }

    public sealed class MultiCommandListener :
        IActorBehaviour,
        IActorCommandListener<TestCommand>,
        IActorCommandListener<AlternateTestCommand>
    {
        public IActor Owner { get; set; }
        public int PrimaryCount { get; private set; }
        public int AlternateCount { get; private set; }

        public void ReactActorCommand(TestCommand command) => PrimaryCount++;
        public void ReactActorCommand(AlternateTestCommand command) => AlternateCount++;
    }

    public sealed class FirstListener : IActorBehaviour, IActorCommandListener<TestCommand>
    {
        public IActor Owner { get; set; }
        public int CommandCount { get; private set; }
        public int CleanUpCount { get; private set; }

        public void ReactActorCommand(TestCommand command) => CommandCount++;
        public void CleanUp() => CleanUpCount++;
    }

    public sealed class SecondListener : IActorBehaviour, IActorCommandListener<TestCommand>
    {
        public IActor Owner { get; set; }
        public int CommandCount { get; private set; }

        public void ReactActorCommand(TestCommand command) => CommandCount++;
    }

    public sealed class ThrowingListener : IActorBehaviour, IActorCommandListener<TestCommand>
    {
        public IActor Owner { get; set; }

        public void ReactActorCommand(TestCommand command) =>
            throw new InvalidOperationException("Command failure.");
    }

    public sealed class SelfRemovingCommandListener : IActorBehaviour, IActorCommandListener<TestCommand>
    {
        public IActor Owner { get; set; }
        public int CommandCount { get; private set; }

        public void ReactActorCommand(TestCommand command)
        {
            CommandCount++;
            Owner.RemoveBehaviour<SelfRemovingCommandListener>();
        }
    }

    public sealed class SelfRemovingBehaviour : IActorBehaviour, IActorTick
    {
        public IActor Owner { get; set; }
        public int TickCount { get; private set; }
        public int CleanUpCount { get; private set; }

        public void Tick(float deltaTime)
        {
            TickCount++;
            Owner.RemoveBehaviour<SelfRemovingBehaviour>();
        }

        public void CleanUp() => CleanUpCount++;
    }

    public sealed class CountingBehaviour : IActorBehaviour, IActorTick
    {
        public IActor Owner { get; set; }
        public int TickCount { get; private set; }

        public void Tick(float deltaTime) => TickCount++;
    }

    public sealed class AllocationBehaviour : IActorBehaviour, IActorTick, IActorCommandListener<TestCommand>
    {
        public IActor Owner { get; set; }
        public int Count { get; private set; }

        public void Tick(float deltaTime) => Count++;
        public void ReactActorCommand(TestCommand command) => Count++;
    }

    public sealed class DestroyOnInitializeBehaviour : IActorBehaviour
    {
        public IActor Owner { get; set; }
        public int CleanUpCount { get; private set; }

        public void Initialize() => Owner.Destroy();
        public void CleanUp() => CleanUpCount++;
    }

    [Serializable]
    public sealed class CloneBehaviour : IActorBehaviour, ICloneable
    {
        [SerializeField] private int _value = 7;

        public IActor Owner { get; set; }
        public int Value => _value;

        public object Clone() => new CloneBehaviour { _value = _value };
    }

    public sealed class CloneBehaviourProvider : AbstractActorBehaviourProvider<CloneBehaviour>
    {
    }

    [Serializable]
    public sealed class SerializedData : IActorData
    {
        [SerializeField] private int _value = 17;

        public int Value => _value;
    }

    public sealed class SerializedDataProvider : AbstractActorDataProvider<SerializedData>
    {
    }
}
