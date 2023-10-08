using System;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of an engine starting / stopping.
    ///     Plays while start is active.
    ///     Clip at index 0 should be an engine starting sound, clip at 1 should be an engine stopping sound (optional).
    /// </summary>
    [Serializable]
    public partial class EngineStartComponent : SoundComponent
    {
        public override GameObject ContainerGO => vc.soundManager.engineSourceGO;

        public override AudioMixerGroup AudioMixerGroup => vc.soundManager.engineMixerGroup;

        public override void Initialize()
        {
            base.Initialize();

            vc.powertrain.engine.OnStart.AddListener(PlayStarting);
            vc.powertrain.engine.OnStop.AddListener(PlayStopping);
        }


        public virtual void PlayStarting()
        {
            // Starting and stopping engine sound
            if (Source != null && Clips.Count > 0)
            {
                if (vc.powertrain.engine.starterActive)
                {
                    if (!Source.isPlaying)
                    {
                        SetVolume(baseVolume);
                        Play(0, 0);
                    }
                }
                else
                {
                    if (Source.isPlaying)
                    {
                        Stop();
                    }
                }
            }
        }


        public virtual void PlayStopping()
        {
            if (Source != null && Clips.Count > 1)
            {
                Source.loop = false;
                SetVolume(baseVolume);
                if (!IsPlaying) Play(0, 1);
            }
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);
            baseVolume = 0.2f;

            if (Clip == null)
            {
                AddDefaultClip("EngineStart");
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(EngineStartComponent))]
    public partial class EngineStartComponentDrawer : SoundComponentDrawer
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

