using System;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Effects
{
    /// <summary>
    ///     Single light source. Can be either an emissive mesh or a light.
    /// </summary>
    [Serializable]
    public partial class LightSource
    {
        public enum LightType
        {
            Light,
            Mesh,
        }

        /// <summary>
        ///     Color of the emitted light.
        /// </summary>
        [ColorUsage(true, true)]
        [Tooltip("    Color of the emitted light.")]
        public Color emissionColor;

        /// <summary>
        ///     Light (point/spot/directional/etc.) representing the vehicle light. Will only be used if light type is set to
        ///     Light.
        /// </summary>
        [Tooltip(
            "Light (point/spot/directional/etc.) representing the vehicle light. Will only be used if light type is set to\r\nLight.")]
        public Light light;

        /// <summary>
        ///     Mesh renderer using standard shader. Emission on the material will be turned on or off depending on light state.
        ///     Will only be used if light type is set to Mesh.
        /// </summary>
        [Tooltip(
            "Mesh renderer using standard shader. Emission on the material will be turned on or off depending on light state.")]
        public MeshRenderer meshRenderer;

        /// <summary>
        ///     If your mesh has more than one material set this number to the index of required material.
        /// </summary>
        [Tooltip("    If your mesh has more than one material set this number to the index of required material.")]
        public int rendererMaterialIndex;

        /// <summary>
        ///     Type of the light.
        /// </summary>
        [Tooltip("    Type of the light.")]
        public LightType type;

        /// <summary>
        ///     Called when the light is turned on.
        /// </summary>
        [NonSerialized] public UnityEvent onLightTurnedOn = new UnityEvent();

        /// <summary>
        ///     Called when the light is turned off.
        /// </summary>
        [NonSerialized] public UnityEvent onLightTurnedOff = new UnityEvent();

        /// <summary>
        ///     Is the light currently on?
        /// </summary>
        public bool IsOn { get; private set; } = false;


        public virtual void TurnOff()
        {
            if (!IsOn)
            {
                return;
            }
            else
            {
                onLightTurnedOff.Invoke();
            }

            if (type == LightType.Light && light != null)
            {
                light.enabled = false;
            }
            else if (Application.isPlaying)
            {
                if (meshRenderer == null || meshRenderer.material == null)
                {
                    return;
                }

                Material mat = meshRenderer.materials[rendererMaterialIndex];

#if NWH_HDRP
                mat.SetFloat("_UseEmissiveIntensity", 0f);
                mat.SetColor("_EmissiveColor", emissionColor * 0f);
#else
                mat.DisableKeyword("_EMISSION");
#endif

                meshRenderer.materials[rendererMaterialIndex].DisableKeyword("_EMISSION");
            }

            IsOn = false;
        }


        public virtual void TurnOn()
        {
            if (IsOn)
            {
                return;
            }
            else
            {
                onLightTurnedOn.Invoke();
            }

            if (type == LightType.Light && light != null)
            {
                light.enabled = true;
            }
            else if (Application.isPlaying)
            {
                if (meshRenderer == null || meshRenderer.material == null)
                {
                    return;
                }

                Material mat = meshRenderer.materials[rendererMaterialIndex];


#if NWH_HDRP
                    float emissionIntensity = 100000f;
                    mat.SetFloat("_UseEmissiveIntensity", 1f);
                    mat.SetColor("_EmissiveColor", emissionColor * emissionIntensity);
#else
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
#endif
            }

            IsOn = true;
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Effects
{
    [CustomPropertyDrawer(typeof(LightSource))]
    public partial class LightSourceDrawer : NUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }


            drawer.BeginSubsection("Light Source");

            drawer.Field("type");
            if ((LightSource.LightType)drawer.FindProperty("type").enumValueIndex == LightSource.LightType.Light)
            {
                drawer.Field("light");
            }
            else
            {
                drawer.Field("meshRenderer");
                drawer.Field("rendererMaterialIndex");
                drawer.Field("emissionColor");
                drawer.Info(
                    "Make sure to tick 'Emission' under the material settings or otherwise the emissive shader variant will not be included in the build.");
            }

            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
