using System;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Vehicle horn sound.
    /// </summary>
    [Serializable]
    public partial class HornComponent : SoundComponent
    {
        public override GameObject ContainerGO => vc.soundManager.otherSourceGO;

        public override AudioMixerGroup AudioMixerGroup => vc.soundManager.otherMixerGroup;

        public override bool InitLoop => true;


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            if (Source != null && Clip != null)
            {
                if (vc.input.Horn && !Source.isPlaying)
                {
                    SetVolume(baseVolume);
                    if (!IsPlaying) Play();
                }
                else if (!vc.input.Horn && Source.isPlaying)
                {
                    Stop();
                }
            }
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            if (Clip == null)
            {
                AddDefaultClip("Horn");
            }
        }
    }
}



#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(HornComponent))]
    public partial class HornComponentDrawer : SoundComponentDrawer
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
