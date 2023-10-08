using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Powertrain
{
    /// <summary>
    ///     A class representing a single ground surface type.
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "NWH Vehicle Physics 2", menuName = "NWH/Vehicle Physics 2/Gearing Profile", order = 1)]
    public partial class TransmissionGearingProfile : ScriptableObject
    {
        /// <summary>
        ///     List of forward gear ratios starting from 1st forward gear.
        /// </summary>
        [SerializeField]
        [Tooltip("    List of forward gear ratios starting from 1st forward gear.")]
        public List<float> forwardGears = new List<float> { 8f, 5.5f, 4f, 3f, 2.2f, 1.7f, 1.3f, };

        /// <summary>
        ///     List of reverse gear ratios starting from 1st reverse gear.
        /// </summary>
        [SerializeField]
        [Tooltip("    List of reverse gear ratios starting from 1st reverse gear.")]
        public List<float> reverseGears = new List<float> { -5f, };
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Powertrain
{
    [CustomEditor(typeof(TransmissionGearingProfile))]
    [CanEditMultipleObjects]
    public partial class TransmissionGearingProfileEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            drawer.BeginSubsection("Gear Ratios");
            drawer.ReorderableList("reverseGears");
            drawer.Label("Neutral: \t 0");
            drawer.Space();
            drawer.ReorderableList("forwardGears");
            drawer.EndSubsection();

            drawer.EndEditor(this);
            return true;
        }
    }
}

#endif
