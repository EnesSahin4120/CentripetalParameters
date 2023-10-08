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
    ///     Sound produced by tire skidding over surface.
    /// </summary>
    [Serializable]
    public partial class WheelSkidComponent : SoundComponent
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
                WheelComponent wheelComponent = vc.Wheels[index];
                CreateAndRegisterAudioSource(AudioMixerGroup, wheelComponent.wheelUAPI.transform.gameObject);
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

            float newVolume = 0f;
            float newPitch = 1f;

            if (vc.groundDetection != null)
            {
                for (int i = 0; i < _wheelCount; i++)
                {
                    WheelComponent wheelComponent = vc.Wheels[i];
                    SurfacePreset surfacePreset = wheelComponent.surfacePreset;

                    bool isSkidding = wheelComponent.wheelUAPI.IsSkiddingLaterally || wheelComponent.wheelUAPI.IsSkiddingLongitudinally;
                    if (wheelComponent.wheelUAPI.IsGrounded && surfacePreset != null && surfacePreset.playSkidSounds && isSkidding)
                    {
                        float slipPercent = wheelComponent.wheelUAPI.NormalizedLateralSlip +
                                            wheelComponent.wheelUAPI.NormalizedLongitudinalSlip;
                        slipPercent = slipPercent < 0 ? 0 : slipPercent > 1 ? 1 : slipPercent;

                        if (surfacePreset.skidSoundClip != null)
                        {
                            AudioSource source = Sources[i];

                            if (!source.isPlaying)
                            {
                                source.Play();
                            }

                            if (source.clip != surfacePreset.skidSoundClip)
                            {
                                // Change skid clip
                                source.clip = surfacePreset.skidSoundClip;
                                source.time = Random.Range(0f, surfacePreset.skidSoundClip.length);
                                source.time = Random.Range(0f, source.clip.length);
                            }

                            float absAngVel = wheelComponent.wheelUAPI.AngularVelocity;
                            float speedCoeff = vc.Speed / 3f + absAngVel / 20f;
                            speedCoeff = speedCoeff > 1f ? 1f : speedCoeff;
                            newVolume = slipPercent * surfacePreset.skidSoundVolume * speedCoeff;
                            newVolume = Mathf.Lerp(_prevVolume[i], newVolume, vc.deltaTime * 80f);

                            float loadCoeff = wheelComponent.wheelUAPI.Load / wheelComponent.wheelUAPI.MaxLoad;
                            loadCoeff = loadCoeff > 1f ? 1f : loadCoeff;
                            newPitch = surfacePreset.skidSoundPitch + loadCoeff * 0.3f;
                            newPitch = Mathf.Lerp(_prevPitch[i], newPitch, vc.deltaTime * 80f);
                        }
                    }
                    else
                    {
                        newVolume = 0f;
                        newPitch = 1f;
                    }

                    SetVolume(newVolume, i);
                    SetPitch(newPitch, i);

                    _prevVolume[i] = newVolume;
                    _prevPitch[i] = newPitch;
                }
            }
        }

        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.5f;

            // Clip set throught surface map system
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(WheelSkidComponent))]
    public partial class WheelSkidComponentDrawer : SoundComponentDrawer
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
