using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Imitates brake hiss on vehicles with pneumatic brake systems such as trucks and buses.
    ///     Accepts multiple clips of which one will be chosen at random each time this effect is played.
    /// </summary>
    [Serializable]
    public partial class AirBrakeComponent : SoundComponent
    {
        public override GameObject ContainerGO => vc.soundManager.otherSourceGO;

        public override AudioMixerGroup AudioMixerGroup => vc.soundManager.otherMixerGroup;

        /// <summary>
        ///     Minimum time between two plays.
        /// </summary>
        [Tooltip("    Minimum time between two plays.")]
        public float minInterval = 4f;

        private float _timer;


        public override void Initialize()
        {
            base.Initialize();

            CreateAndRegisterAudioSource(vc.soundManager.otherMixerGroup, vc.soundManager.otherSourceGO);

            vc.brakes.onBrakesDeactivate.AddListener(PlayBrakeHiss);
        }


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            _timer += Time.deltaTime;
        }


        public override void Enable()
        {
            base.Enable();
            vc.brakes.onBrakesDeactivate.AddListener(PlayBrakeHiss);
        }


        public override void Disable()
        {
            base.Disable();
            vc.brakes.onBrakesDeactivate.RemoveListener(PlayBrakeHiss);
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.1f;

            if (Clip == null)
            {
                AddDefaultClip("AirBrakes");
            }
        }


        public void PlayBrakeHiss()
        {
            if (_timer < minInterval || Clip == null || !vc.powertrain.engine.IsRunning)
            {
                return;
            }

            Source.clip = RandomClip;
            SetVolume(Random.Range(0.8f, 1.2f) * baseVolume);
            if (!IsPlaying) Play();

            _timer = 0f;
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(AirBrakeComponent))]
    public partial class AirBrakeComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("minInterval", true, "s");

            drawer.EndProperty();
            return true;
        }
    }
}
#endif

