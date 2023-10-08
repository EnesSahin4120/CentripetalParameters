using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [Serializable]
    public partial class ReverseBeepComponent : SoundComponent
    {
        public override GameObject ContainerGO => vc.soundManager.otherSourceGO;

        public override AudioMixerGroup AudioMixerGroup => vc.soundManager.otherMixerGroup;

        public override bool InitLoop => true;


        public bool beepOnNegativeVelocity = true;
        public bool beepOnReverseGear = true;


        public override void Enable()
        {
            base.Enable();

            vc.StopCoroutine(BeepCoroutine());
            vc.StartCoroutine(BeepCoroutine());
        }

        public override void Disable()
        {
            base.Disable();

            vc.StopCoroutine(BeepCoroutine());
        }

        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            if (Clip == null)
            {
                AddDefaultClip("ReverseBeep");
            }
        }

        IEnumerator BeepCoroutine()
        {
            while (true)
            {
                int gear = vc.powertrain.transmission.Gear;
                if (beepOnReverseGear && gear < 0 ||
                    beepOnNegativeVelocity && vc.LocalForwardVelocity < -0.2f && gear <= 0)
                {
                    SetVolume(baseVolume);
                    Play();
                }
                else
                {
                    Stop();
                }

                yield return new WaitForSeconds(1f);
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(ReverseBeepComponent))]
    public partial class ReverseBeepComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("beepOnReverseGear");
            drawer.Field("beepOnNegativeVelocity");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif

