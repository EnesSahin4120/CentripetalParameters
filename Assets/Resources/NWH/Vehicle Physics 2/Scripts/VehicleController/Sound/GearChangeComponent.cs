using System;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Shifter sound played when changing gears.
    ///     Supports multiple audio clips of which one is chosen at random each time this effect is played.
    /// </summary>
    [Serializable]
    public partial class GearChangeComponent : SoundComponent
    {
        public override GameObject ContainerGO => vc.soundManager.transmissionSourceGO;

        public override AudioMixerGroup AudioMixerGroup => vc.soundManager.transmissionMixerGroup;

        public override void Enable()
        {
            base.Enable();

            vc.powertrain.transmission.onShift.AddListener(PlayShiftSound);
        }


        public override void Disable()
        {
            base.Disable();

            vc.powertrain.transmission.onShift.RemoveListener(PlayShiftSound);
        }


        private void PlayShiftSound(GearShift gearShift)
        {
            if (!IsEnabled || !vc.soundManager.IsEnabled)
            {
                return;
            }

            if (gearShift.ToGear != 0)
            {
                SetVolume(baseVolume);
                PlayRandomClip();
            }
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.16f;

            if (Clip == null)
            {
                AddDefaultClip("GearChange");
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(GearChangeComponent))]
    public partial class GearChangeComponentDrawer : SoundComponentDrawer
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

