using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.GroundDetection
{
    /// <summary>
    ///     Maps SurfacePreset to the terrain texture indices and object tags.
    /// </summary>
    [Serializable]
    public partial class SurfaceMap
    {
        /// <summary>
        ///     Name of the surface map. For display purposes only.
        /// </summary>
        [Tooltip("    Name of the surface map. For display purposes only.")]
        public string name;

        public SurfacePreset surfacePreset;

        /// <summary>
        ///     Objects with tags in this list will be recognized as this type of surface.
        /// </summary>
        [Tooltip("    Objects with tags in this list will be recognized as this type of surface.")]
        public List<string> tags = new List<string>();

        /// <summary>
        ///     Indices of terrain textures that represent this type of surface. Starts with 0 with the first texture being in the
        ///     top left corner
        ///     under terrain settings - Paint Texture.
        /// </summary>
        [Tooltip(
            "Indices of terrain textures that represent this type of surface. Starts with 0 with the first texture being in the top left corner " +
            "under terrain settings - Paint Texture.")]
        public List<int> terrainTextureIndices = new List<int>();
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.GroundDetection
{
    [CustomPropertyDrawer(typeof(SurfaceMap))]
    public partial class SurfaceMapDrawer : NVP_NUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("name");
            drawer.Field("surfacePreset");
            drawer.ReorderableList("terrainTextureIndices");
            drawer.ReorderableList("tags");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
