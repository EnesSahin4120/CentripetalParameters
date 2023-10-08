using System;
using System.Collections.Generic;
using UnityEngine;

namespace NWH.WheelController3D
{
    [DefaultExecutionOrder(105)]
    public class WheelControllerManager : MonoBehaviour
    {
        [HideInInspector]
        [NonSerialized]
        public List<WheelController> wheelControllers = new List<WheelController>();
    }
}
