using System;
using System.Collections.Generic;
using NWH.Common.Vehicles;
using UnityEngine;
using UnityEngine.Audio;
using NWH.VehiclePhysics2.Powertrain;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of wheel hitting the surface or obstracle.
    /// </summary>
    [Serializable]
    public partial class SuspensionBumpComponent : SoundComponent
    {
        public override GameObject ContainerGO => vc.soundManager.transmissionSourceGO;

        public override AudioMixerGroup AudioMixerGroup => vc.soundManager.transmissionMixerGroup;


        private List<bool> wheelWasGrounded = new List<bool>();


        public override void Initialize()
        {
            base.Initialize();

            wheelWasGrounded = new List<bool>();
            foreach (WheelComponent wheelComponent in vc.Wheels)
            {
                wheelWasGrounded.Add(true);
            }
        }


        public override void FixedUpdate()
        {
            if (!Active || vc.realtimeSinceStartup < 2f) // Dont play initially to prevent bumps on vehicle spawn.
            {
                return;
            }

            for (int i = 0; i < vc.Wheels.Count; i++)
            {
                bool isGrounded = vc.Wheels[i].wheelUAPI.IsGrounded;
                bool wasGrounded = wheelWasGrounded[i];
                if (isGrounded && !wasGrounded)
                {
                    PlayBumpSound(vc.Wheels[i].wheelUAPI);
                }

                wheelWasGrounded[i] = isGrounded;
            }
        }



        private void PlayBumpSound(WheelUAPI wheel)
        {
            float newPitch = UnityEngine.Random.Range(0.7f, 1.3f);
            float newVolume = baseVolume * Mathf.Clamp01(wheel.Load / wheel.MaxLoad);

            SetPitch(newPitch);
            SetVolume(newVolume);
            PlayRandomClip();
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.4f;

            if (Clip == null)
            {
                AddDefaultClip("SuspensionBump");
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(SuspensionBumpComponent))]
    public partial class SuspensionBumpComponentDrawer : SoundComponentDrawer
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
