using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of a wastegate releasing air on turbocharged vehicles.
    /// </summary>
    [Serializable]
    public partial class TurboFlutterComponent : SoundComponent
    {
        public override GameObject ContainerGO => vc.soundManager.engineSourceGO;

        public override AudioMixerGroup AudioMixerGroup => vc.soundManager.engineMixerGroup;


        public override void Initialize()
        {
            base.Initialize();

            vc.powertrain.engine.forcedInduction.onWastegateRelease.AddListener(PlayFlutterSound);
        }


        private void PlayFlutterSound()
        {
            if (!IsPlaying && Clip != null)
            {
                float newVolume = baseVolume * vc.powertrain.engine.forcedInduction.wastegateBoost * Random.Range(0.7f, 1.3f);
                SetVolume(Mathf.Clamp01(newVolume));
                PlayRandomClip();

            }
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.015f;

            if (Clip == null)
            {
                AddDefaultClip("TurboFlutter");
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(TurboFlutterComponent))]
    public partial class TurboFlutterComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }


            drawer.EndProperty();
            return true;
        }
    }
}

#endif
