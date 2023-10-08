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
    ///     Sound of exhaust popping.
    ///     Requires exhaust flash to be enabled to work.
    /// </summary>
    [Serializable]
    public partial class ExhaustPopComponent : SoundComponent
    {
        public override GameObject ContainerGO => vc.soundManager.exhaustSourceGO;

        public override AudioMixerGroup AudioMixerGroup => vc.soundManager.engineMixerGroup;

        public override bool InitLoop => false;



        public enum PopSource
        {
            RevLimiter,
            ExhaustFlash,
        }

        /// <summary>
        /// The source for the pop trigger. 
        /// If ExhaustFlash is selected, ExhaustFlash effect needs to be set up for this to work.
        /// </summary>
        public PopSource popSource = PopSource.ExhaustFlash;

        /// <summary>
        /// Each time there is an exhaust flash or rev limiter is hit, what is the chance of exhaust pop?
        /// </summary>
        public float popChance = 0.1f;

        /// <summary>
        /// Should pops happen randomly when the vehicle is decelerating with throttle released.
        /// </summary>
        public bool popOnDeceleration = true;

        /// <summary>
        /// The amount of pops under deceleration.
        /// </summary>
        public float decelerationPopChanceCoeff = 1f;


        public override void Initialize()
        {
            base.Initialize();

            if (popSource == PopSource.RevLimiter)
            {
                vc.powertrain.engine.onRevLimiter.AddListener(Pop);
            }
            else if (popSource == PopSource.ExhaustFlash)
            {
                vc.effectsManager.exhaustFlash.onFlash.AddListener(Pop);
            }
        }


        public override void FixedUpdate()
        {
            if (!Active)
            {
                return;
            }

            if (popOnDeceleration && vc.powertrain.engine.ThrottlePosition < 0.1f && vc.powertrain.engine.RPMPercent > 0.4f)
            {
                if (Random.Range(0f, 1f) < popChance * decelerationPopChanceCoeff * vc.fixedDeltaTime)
                {
                    SetVolume(baseVolume * 0.5f + vc.powertrain.engine.RPMPercent * 0.5f);
                    Pop();
                }
            }
        }


        public void Pop()
        {
            if (Random.Range(0f, 1f) > popChance)
            {
                return;
            }

            Stop();
            SetVolume(Random.Range(baseVolume * 0.5f, baseVolume * 1.5f));
            SetPitch(Random.Range(0.7f, 1.3f));
            PlayRandomClip();
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.3f;

            if (Clip == null)
            {
                AddDefaultClip("ExhaustPop");
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(ExhaustPopComponent))]
    public partial class ExhaustPopComponentDrawer : SoundComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("popSource");
            drawer.Field("popChance");
            drawer.Field("popOnDeceleration");
            drawer.Field("decelerationPopChanceCoeff");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif


