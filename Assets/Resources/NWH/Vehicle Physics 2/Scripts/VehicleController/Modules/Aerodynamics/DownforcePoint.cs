using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.Aerodynamics
{
    /// <summary>
    ///     Single point at which downforce will be applied.
    /// </summary>
    [Serializable]
    public partial class DownforcePoint
    {
        /// <summary>
        ///     Maximim force in [N] that can be applied as a result of downforce.
        ///     Putting in a too large value will make the vehicle bottom out at high speeds if suspension is too soft.
        /// </summary>
        [Tooltip(
            "Maximim force in [N] that can be applied as a result of downforce.\r\nPutting in a too large value will make the vehicle bottom out at high speeds if suspension is too soft.")]
        public float maxForce;

        /// <summary>
        ///     Position relative to the vehicle at which downforce will be applied. Marked by red arrow gizmo.
        ///     Y component should be at about the spring anchor height (i.e. WheelController position).
        /// </summary>
        [Tooltip(
            "Position relative to the vehicle at which downforce will be applied. Marked by red arrow gizmo.\r\nY component should be at about the spring anchor height (i.e. WheelController position).")]
        public Vector3 position;
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.Aerodynamics
{
    [CustomPropertyDrawer(typeof(DownforcePoint))]
    public partial class DownforcePointDrawer : NVP_NUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("position");
            drawer.Field("maxForce");

            drawer.EndProperty();
            return true;
        }
    }
}
#endif
