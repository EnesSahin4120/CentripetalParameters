using System;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of an engine idling.
    /// </summary>
    [Serializable]
    public partial class EngineRunningComponent : SoundComponent
    {
        public override GameObject ContainerGO => vc.soundManager.engineSourceGO;

        public override AudioMixerGroup AudioMixerGroup => vc.soundManager.engineMixerGroup;

        /// <summary>
        ///     Distortion at maximum engine load.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Distortion at maximum engine load.")]
        public float maxDistortion = 0.4f;

        /// <summary>
        ///     Pitch added to the base engine pitch depending on engine RPM.
        /// </summary>
        [Range(0, 4)]
        [Tooltip("    Pitch added to the base engine pitch depending on engine RPM.")]
        public float pitchRange = 2f;

        /// <summary>
        ///     Smoothing of engine volume.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Smoothing of engine volume.")]
        public float smoothing = 0.05f;

        /// <summary>
        ///     Volume added to the base engine volume depending on engine state.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Volume added to the base engine volume depending on engine state.")]
        public float volumeRange = 0.1f;

        private float _volume;
        private float _volumeVelocity;
        private float _distortion;
        private float _distortionVelocity;

        public override bool InitLoop => true;


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            // Engine sound
            if (Source != null && Clip != null)
            {
                if (vc.powertrain.engine.IsRunning || vc.powertrain.engine.starterActive)
                {
                    if (!Source.isPlaying && Source.enabled)
                    {
                        Play();
                    }

                    float newPitch = vc.powertrain.engine.RPMPercent * pitchRange;
                    float throttleInput = vc.powertrain.engine.revLimiterActive
                                              ? 1f
                                              : vc.powertrain.engine.ThrottlePosition;
                    SetPitch(newPitch);

                    float newDistortion = throttleInput * maxDistortion;
                    _distortion = Mathf.SmoothDamp(_distortion, newDistortion, ref _distortionVelocity, smoothing);
                    Source.outputAudioMixerGroup.audioMixer.SetFloat("engineDistortion", _distortion);

                    float newVolume = baseVolume;
                    if (!vc.powertrain.engine.starterActive)
                    {
                        newVolume += (throttleInput * 0.5f + vc.powertrain.engine.RPMPercent * 0.5f) * volumeRange;
                        newVolume -= _distortion * 0.5f; // Distortion increases volume, counter that.
                        newVolume = Mathf.Clamp(newVolume, baseVolume, 10f);

                    }

                    _volume = Mathf.SmoothDamp(_volume, newVolume, ref _volumeVelocity, smoothing);
                    SetVolume(_volume);
                }
                else
                {
                    if (Source.isPlaying)
                    {
                        Stop();
                    }

                    SetVolume(0);
                    SetPitch(0);
                }
            }
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.6f;
            volumeRange = 0.4f;
            pitchRange = 1.8f;

            if (Clip == null)
            {
                AddDefaultClip("EngineRunning");
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(EngineRunningComponent))]
    public partial class EngineRunningComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("volumeRange");
            drawer.Field("pitchRange");
            drawer.Field("smoothing");
            drawer.Field("maxDistortion");

            drawer.EndProperty();
            return true;
        }
    }
}
#endif
