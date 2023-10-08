using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.GroundDetection
{
    /// <summary>
    ///     A ScriptableObject representing a set of SurfaceMaps. Usually one per scene or project.
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "NWH Vehicle Physics 2", menuName = "NWH/Vehicle Physics 2/Ground Detection Preset",
                     order = 1)]
    public partial class GroundDetectionPreset : ScriptableObject
    {
        /// <summary>
        ///     Prefab of the particle system for generating dust (gravel, sand, etc.) or smoke (asphalt, concrete, etc.) generated
        ///     by the wheels.
        /// </summary>
        [FormerlySerializedAs("dustPrefab")]
        [Tooltip(
            "    Prefab of the particle system for generating dust as a result of traveling over sand, gravel, etc.")]
        public GameObject particlePrefab;

        /// <summary>
        ///     Prefab of the particle system for generating surface chunks / dirt that gets thrown behind the wheel when going
        ///     over soft surface.
        /// </summary>
        [Tooltip(
            "    Prefab of the particle system for generating surface chunks / dirt that gets thrown behind the wheel when going over soft surface.")]
        public GameObject chunkPrefab;

        /// <summary>
        ///     Surface preset used when there are no matches in the surfaceMaps list for the current surface.
        /// </summary>
        [Tooltip("    Surface preset used when there are no matches in the surfaceMaps list for the current surface.")]
        public SurfacePreset fallbackSurfacePreset;

        /// <summary>
        ///     Surface maps - each represents a single ground surface.
        /// </summary>
        [Tooltip("    Surface maps - each represents a single ground surface.")]
        public List<SurfaceMap> surfaceMaps = new List<SurfaceMap>();
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.GroundDetection
{
    [CustomEditor(typeof(GroundDetectionPreset))]
    [CanEditMultipleObjects]
    public partial class GroundDetectionPresetEditor : NVP_NUIEditor
    {
        private GroundDetectionPreset preset;


        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            preset = (GroundDetectionPreset)target;

            drawer.BeginSubsection("Particles");
            drawer.Field("particlePrefab");
            drawer.Field("chunkPrefab");
            drawer.EndSubsection();

            drawer.BeginSubsection("Surface Maps");
            drawer.Field("fallbackSurfacePreset");
            drawer.ReorderableList("surfaceMaps");
            drawer.EndSubsection();

            drawer.EndEditor(this);
            return true;
        }
    }
}

#endif
