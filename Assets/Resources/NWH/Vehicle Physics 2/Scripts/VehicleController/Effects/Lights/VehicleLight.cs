using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Effects
{
    /// <summary>
    ///     Single vehicle light.
    /// </summary>
    [Serializable]
    public partial class VehicleLight
    {
        /// <summary>
        ///     All the light sources representing the vehicle light.
        ///     E.g. low beam can be represented by a directional light to represent light beam and
        ///     and emissive mesh to represent light optics.
        /// </summary>
        [Tooltip(
            "    All the light sources representing the vehicle light.\r\n    E.g. low beam can be represented by a directional light to represent light beam and\r\n    and emissive mesh to represent light optics.")]
        public List<LightSource> lightSources = new List<LightSource>();

        protected bool isOn;

        /// <summary>
        ///     State of the light.
        /// </summary>
        public bool On
        {
            get { return isOn; }
            set { isOn = value; }
        }


        public void SetState(bool state)
        {
            if (state)
            {
                TurnOn();
            }
            else
            {
                TurnOff();
            }
        }


        public void Toggle()
        {
            if (isOn)
            {
                TurnOff();
            }
            else
            {
                TurnOn();
            }
        }


        /// <summary>
        ///     Turns off the light source or disables emission on the mesh. Mesh is required to have standard shader.
        /// </summary>
        public void TurnOff()
        {
            for (int i = 0; i < lightSources.Count; i++)
            {
                LightSource source = lightSources[i];
                source.TurnOff();
            }

            isOn = false;
        }


        /// <summary>
        ///     Turns on the light source or enables emission on the mesh. Mesh is required to have standard shader.
        /// </summary>
        public void TurnOn()
        {
            for (int i = 0; i < lightSources.Count; i++)
            {
                LightSource source = lightSources[i];
                source.TurnOn();
            }

            isOn = true;
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Effects
{
    [CustomPropertyDrawer(typeof(VehicleLight))]
    public partial class VehicleLightDrawer : NUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.BeginSubsection(property.displayName);
            drawer.ReorderableList("lightSources");
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
