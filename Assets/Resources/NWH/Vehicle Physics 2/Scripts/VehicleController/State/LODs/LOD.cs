using System;
using UnityEngine;

#if UNITY_EDITOR
using NWH.NUI;
using UnityEditor;
#endif

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     ScriptableObject representing settings for a single LOD.
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "NWH Vehicle Physics 2", menuName = "NWH/Vehicle Physics 2/LOD",
                     order = 1)]
    public partial class LOD : ScriptableObject
    {
        public float distance;
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.GroundDetection
{
    /// <summary>
    ///     Editor for LOD.
    /// </summary>
    [CustomEditor(typeof(LOD))]
    [CanEditMultipleObjects]
    public partial class LODEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            drawer.BeginSubsection("Settings");
            drawer.Field("distance");
            drawer.EndSubsection();

            drawer.EndEditor(this);
            return true;
        }
    }
}
#endif
