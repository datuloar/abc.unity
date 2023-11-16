using abc.unity.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoUpdater : MonoBehaviour
{
    private static HashSet<ITickable> _tickables = new(1000);
    private static HashSet<IFixedTickable> _fixedTickables = new(1000);
    private static HashSet<ILateTickable> _lateTickables = new(1000);



}
