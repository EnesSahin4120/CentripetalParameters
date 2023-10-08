using System;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     EngineFanComponent is used to imitate engine fan running, the sound especially prominent in commercial vehicles and
    ///     off-road vehicles with clutch driven fan.
    ///     Can also be used to mimic induction noise.
    /// </summary>
    [Serializable]
    public partial class EngineFanComponent : SoundComponent
    {
        public override GameObject ContainerGO => vc.soundManager.engineSourceGO;

        public override AudioMixerGroup AudioMixerGroup => vc.soundManager.engineMixerGroup;

        /// <summary>
        /// Starting sound pitch at idle RPM.
        /// </summary>
        [UnityEngine.Tooltip("Starting sound pitch at idle RPM.")]
        public float basePitch = 1f;

        /// <summary>
        /// Pitch range, redline pitch equals basePitch + pitchRange.
        /// </summary>
        [Range(0, 4)]
        [UnityEngine.Tooltip("Pitch range, redline pitch equals basePitch + pitchRange.")]
        public float pitchRange = 0.5f;

        public override bool InitLoop => true;


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            if (vc.powertrain.engine.IsRunning)
            {
                if (!Source.isPlaying)
                {
                    Play();
                }

                float rpmPercent = vc.powertrain.engine.RPMPercent;
                SetVolume(rpmPercent * rpmPercent * baseVolume);
                SetPitch(basePitch + pitchRange * rpmPercent);
            }
            else
            {
                if (Source.isPlaying)
                {
                    Stop();
                }
            }
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.05f;

            if (Clip == null)
            {
                AddDefaultClip("EngineFan");
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(EngineFanComponent))]
    public partial class EngineFanComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("basePitch");
            drawer.Field("pitchRange");

            drawer.EndProperty();
            return true;
        }
    }
}
#endif

