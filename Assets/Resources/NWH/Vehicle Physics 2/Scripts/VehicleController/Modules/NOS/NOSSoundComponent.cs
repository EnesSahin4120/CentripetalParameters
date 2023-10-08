using System;
using NWH.VehiclePhysics2.Sound.SoundComponents;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.NOS
{
    /// <summary>
    ///     Sound component producing the distinct 'hiss' sound of active NOS.
    /// </summary>
    [Serializable]
    public partial class NOSSoundComponent : SoundComponent
    {
        public override GameObject ContainerGO => vc.soundManager.engineSourceGO;

        public override AudioMixerGroup AudioMixerGroup => vc.soundManager.engineMixerGroup;


        [NonSerialized]
        public NOSModule nosModule;

        public override bool InitLoop => true;


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            if (nosModule.IsUsingNOS)
            {
                SetVolume(baseVolume);
                if (!IsPlaying) Play();
            }
            else
            {
                Stop();
            }
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.2f;

            if (Clip == null)
            {
                Clip = Resources.Load(VehicleController.defaultResourcesPath + "Sound/NOS") as AudioClip;
                if (Clip == null)
                {
                    Debug.LogWarning(
                        $"Audio Clip for sound component {GetType().Name}  from resources. Source will not play.");
                }
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.NOS
{
    [CustomPropertyDrawer(typeof(NOSSoundComponent))]
    public partial class NOSSoundComponentDrawer : SoundComponentDrawer
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
