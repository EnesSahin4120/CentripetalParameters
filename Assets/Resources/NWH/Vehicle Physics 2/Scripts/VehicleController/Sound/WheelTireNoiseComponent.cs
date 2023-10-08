using System;
using NWH.VehiclePhysics2.GroundDetection;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sounds produced by tire rolling over the surface.
    /// </summary>
    [Serializable]
    public partial class WheelTireNoiseComponent : SoundComponent
    {
        public override GameObject ContainerGO => vc.soundManager.otherSourceGO;

        public override AudioMixerGroup AudioMixerGroup => vc.soundManager.surfaceNoiseMixerGroup;

        public override bool InitPlayOnAwake => true;

        public override bool InitLoop => true;


        private float[] _prevPitch;
        private float[] _prevVolume;
        private int _wheelCount;


        public override void Initialize()
        {
            base.Initialize();

            _wheelCount = vc.Wheels.Count;
            for (int index = 0; index < _wheelCount; index++)
            {
                WheelComponent wheel = vc.Wheels[index];
                CreateAndRegisterAudioSource(AudioMixerGroup, wheel.wheelUAPI.transform.gameObject);
            }

            _wheelCount = vc.Wheels.Count;
            _prevVolume = new float[_wheelCount];
            _prevPitch = new float[_wheelCount];
        }

        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            for (int i = 0; i < _wheelCount; i++)
            {
                WheelComponent wheelComponent = vc.Wheels[i];
                SurfacePreset surfacePreset = wheelComponent.surfacePreset;

                float newVolume = 0f;
                float newPitch = 1f;

                if (wheelComponent.wheelUAPI.IsGrounded && surfacePreset != null && surfacePreset.playSurfaceSounds)
                {
                    if (surfacePreset.surfaceSoundClip != null)
                    {
                        AudioSource source = Sources[i];

                        if (!source.isPlaying && source.isActiveAndEnabled)
                        {
                            source.Play();
                        }

                        if (source.clip != surfacePreset.surfaceSoundClip)
                        {
                            // Change skid clip
                            source.clip = surfacePreset.surfaceSoundClip;
                            source.time = Random.Range(0f, surfacePreset.surfaceSoundClip.length);
                            source.time = Random.Range(0f, source.clip.length);
                        }

                        float surfaceModifier = 1f;
                        if (surfacePreset.slipSensitiveSurfaceSound)
                        {
                            surfaceModifier = wheelComponent.wheelUAPI.NormalizedLateralSlip / vc.longitudinalSlipThreshold;
                            surfaceModifier = surfaceModifier < 0 ? 0 : surfaceModifier > 1 ? 1 : surfaceModifier;
                        }

                        float speedCoeff = vc.Speed / 20f;
                        speedCoeff = speedCoeff < 0 ? 0 : speedCoeff > 1 ? 1 : speedCoeff;

                        // Change surface volume and pitch
                        newVolume = surfacePreset.surfaceSoundVolume * surfaceModifier * speedCoeff;
                        newVolume = newVolume < 0 ? 0 : newVolume > 1 ? 1 : newVolume;
                        newVolume = Mathf.Lerp(_prevVolume[i], newVolume, vc.deltaTime * 12f);

                        newPitch = surfacePreset.surfaceSoundPitch * 0.5f + speedCoeff;
                    }
                }
                else
                {
                    newVolume = Mathf.Lerp(_prevVolume[i], 0, vc.deltaTime * 12f);
                    newPitch = Mathf.Lerp(_prevPitch[i], 1f, vc.deltaTime * 12f);
                }

                SetVolume(newVolume, i);
                SetPitch(newPitch, i);

                _prevVolume[i] = newVolume;
                _prevPitch[i] = newPitch;
            }
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.4f;

            // Clip auto-set through surface maps
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(WheelTireNoiseComponent))]
    public partial class WheelTireNoiseComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool initGUIState = GUI.enabled;
            GUI.enabled = false;
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Info("Check SurfaceMaps to change per-surface clips and settings.");


            GUI.enabled = initGUIState;


            drawer.EndProperty();
            return true;
        }
    }
}
#endif
