using System;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of turbocharger or supercharger.
    /// </summary>
    [Serializable]
    public partial class TurboWhistleComponent : SoundComponent
    {
        public override GameObject ContainerGO => vc.soundManager.engineSourceGO;

        public override AudioMixerGroup AudioMixerGroup => vc.soundManager.engineMixerGroup;

        /// <summary>
        ///     Pitch range that will be added to the base pitch depending on turbos's RPM.
        /// </summary>
        [Range(0, 5)]
        [Tooltip("    Pitch range that will be added to the base pitch depending on turbos's RPM.")]
        public float pitchRange = 0.9f;

        public override bool InitLoop => true;


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            if (Clip != null && vc.powertrain.engine.IsRunning &&
                vc.powertrain.engine.forcedInduction.useForcedInduction)
            {
                SetVolume(Mathf.Clamp01(baseVolume
                                        * vc.powertrain.engine.forcedInduction.boost * vc.powertrain.engine.forcedInduction.boost));
                SetPitch(pitchRange * vc.powertrain.engine.forcedInduction.boost);
                if (!Source.isPlaying) Play();
            }
            else
            {
                if (Source != null)
                {
                    SetVolume(0);
                    if (Source.isPlaying) Stop();
                }
            }
        }

        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.015f;
            pitchRange = 0.8f;

            if (Clip == null)
            {
                AddDefaultClip("TurboWhistle");
            }
        }
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(TurboWhistleComponent))]
    public partial class TurboWhistleComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("pitchRange");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
