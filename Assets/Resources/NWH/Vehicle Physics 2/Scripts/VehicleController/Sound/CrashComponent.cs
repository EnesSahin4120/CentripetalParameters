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
    ///     Sound of vehicle crashing into an object.
    ///     Supports multiple audio clips of which one will be chosen at random each time this effect is played.
    /// </summary>
    [Serializable]
    public partial class CrashComponent : SoundComponent
    {
        public override GameObject ContainerGO => vc.soundManager.crashSourceGO;

        public override AudioMixerGroup AudioMixerGroup => vc.soundManager.otherMixerGroup;

        /// <summary>
        ///     Different random pitch in range [basePitch + (1 +- pitchRandomness)] is set each time a collision happens.
        /// </summary>
        [Range(0, 0.5f)]
        [Tooltip(
            "Different random pitch in range [basePitch + (1 +- pitchRandomness)] is set each time a collision happens.")]
        public float pitchRandomness = 0.4f;

        /// <summary>
        ///     Higher values result in collisions getting louder for the given collision velocity magnitude.
        /// </summary>
        [Range(0, 5)]
        [Tooltip("    Higher values result in collisions getting louder for the given collision velocity magnitude.")]
        public float velocityMagnitudeEffect = 1f;

        private Collision collisionData;
        private bool collisionFlag;


        public override void Initialize()
        {
            base.Initialize();

            CreateAndRegisterAudioSource(vc.soundManager.otherMixerGroup, vc.soundManager.crashSourceGO);
            vc.damageHandler.OnCollision.AddListener(RaiseCollisionFlag);
        }


        public override void Update()
        {
            if (!Active)
            {
                collisionFlag = false;
                return;
            }

            if (collisionFlag)
            {
                PlayCollisionSound();
                collisionFlag = false;
            }
        }

        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.4f;

            if (Clip == null)
            {
                AddDefaultClip("Crash");
            }
        }


        public void PlayCollisionSound()
        {
            if (!IsEnabled || !initialized)
            {
                return;
            }

            if (IsPlaying) return;

            if (Clips.Count == 0 || collisionData == null || collisionData.contacts.Length == 0)
            {
                return;
            }

            vc.soundManager.crashSourceGO.transform.position = collisionData.contacts[0].point;

            float newVolume =
                Mathf.Clamp01(collisionData.relativeVelocity.magnitude * 0.2f * velocityMagnitudeEffect) *
                baseVolume;
            newVolume = Mathf.Clamp01(newVolume);
            float newPitch = Random.Range(1f - pitchRandomness, 1f + pitchRandomness);

            SetVolume(newVolume);
            SetPitch(newPitch);
            PlayRandomClip();
        }


        public void RaiseCollisionFlag(Collision collision)
        {
            collisionData = collision;
            collisionFlag = true;
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(CrashComponent))]
    public partial class CrashComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("pitchRandomness");
            drawer.Field("velocityMagnitudeEffect");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif


