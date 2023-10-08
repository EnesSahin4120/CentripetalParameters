using System;
using NWH.Common.Vehicles;
using NWH.VehiclePhysics2.Effects;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Click-clack of the working blinker.
    ///     Accepts two clips, first is for the blinker turning on and the second is for blinker turning off.
    /// </summary>
    [Serializable]
    public partial class BlinkerComponent : SoundComponent
    {
        public override GameObject ContainerGO => vc.soundManager.otherSourceGO;

        public override AudioMixerGroup AudioMixerGroup => vc.soundManager.otherMixerGroup;


        public override void Initialize()
        {
            base.Initialize();

            if (!vc.GetMultiplayerIsRemote())
            {
                if (vc.effectsManager.lightsManager.leftBlinkers.lightSources.Count > 0)
                {
                    LightSource ls = vc.effectsManager.lightsManager.leftBlinkers.lightSources[0];
                    ls.onLightTurnedOn.AddListener(PlayBlinkerOn);
                    ls.onLightTurnedOff.AddListener(PlayBlinkerOff);
                }

                if (vc.effectsManager.lightsManager.rightBlinkers.lightSources.Count > 0)
                {
                    LightSource ls = vc.effectsManager.lightsManager.rightBlinkers.lightSources[0];
                    ls.onLightTurnedOn.AddListener(PlayBlinkerOn);
                    ls.onLightTurnedOff.AddListener(PlayBlinkerOff);
                }
            }
        }


        private void PlayBlinkerOn()
        {
            Source.volume = baseVolume;
            Play(0, 0);
        }


        private void PlayBlinkerOff()
        {
            Source.volume = baseVolume;
            Play(0, 1);
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.8f;

            if (Clip == null)
            {
                AddDefaultClip("BlinkerOn");
                AddDefaultClip("BlinkerOff");
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(BlinkerComponent))]
    public partial class BlinkerComponentDrawer : SoundComponentDrawer
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
