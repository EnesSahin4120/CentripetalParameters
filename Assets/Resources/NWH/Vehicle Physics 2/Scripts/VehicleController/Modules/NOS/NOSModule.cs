using System;
using System.Linq;
using NWH.VehiclePhysics2.Modules.NOS;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules
{
    /// <summary>
    ///     NOS (Nitrous Oxide System) module.
    /// </summary>
    [Serializable]
    public partial class NOSModule : VehicleModule
    {
        /// <summary>
        ///     Capacity of NOS bottle.
        /// </summary>
        [Tooltip("    Capacity of NOS bottle.")]
        public float capacity = 2f;

        /// <summary>
        ///     Current charge of NOS bottle.
        /// </summary>
        [Tooltip("    Current charge of NOS bottle.")]
        public float charge = 2f;

        /// <summary>
        ///     Can NOS be used while in reverse?
        /// </summary>
        [Tooltip("    Can NOS be used while in reverse?")]
        public bool disableInReverse = true;

        /// <summary>
        ///     Can NOS be used while there is no throttle input / engine is idling?
        /// </summary>
        [Tooltip("    Can NOS be used while there is no throttle input / engine is idling?")]
        public bool disableOffThrottle = true;

        /// <summary>
        ///     Makes engine sound louder while NOS is active.
        ///     Volume range of the engine running sound component will get multiplied by this value.
        /// </summary>
        [Range(1, 3)]
        [Tooltip(
            "Makes engine sound louder while NOS is active.\r\nVolume range of the engine running sound component will get multiplied by this value.")]
        public float engineVolumeCoefficient = 1.5f;

        /// <summary>
        ///     Value that will be used as base intensity of Exhaust Smoke effect while NOS is active.
        /// </summary>
        [Range(1, 3)]
        [Tooltip("    Value that will be used as base intensity of Exhaust Smoke effect while NOS is active.")]
        public float exhaustEmissionCoefficient = 2f;

        /// <summary>
        ///     Maximum flow of NOS in kg/s.
        /// </summary>
        [Tooltip("    Maximum flow of NOS in kg/s.")]
        public float flow = 0.1f;

        /// <summary>
        ///     Power of the engine will be multiplied by this value when NOS is active to get the final engine power.
        /// </summary>
        [Range(1, 5)]
        [Tooltip(
            "Power of the engine will be multiplied by this value when NOS is active to get the final engine power.")]
        public float powerCoefficient = 2f;

        [SerializeField]
        public NOSSoundComponent nosSoundComponent = new NOSSoundComponent();

        public bool IsUsingNOS
        {
            get
            {
                return Active && vc.input.Boost && charge > 0;
            }
        }


        public override void Initialize()
        {
            nosSoundComponent.nosModule = this;
            vc.soundManager.RegisterExternalSoundComponent(nosSoundComponent);

            base.Initialize();
        }


        public override void Enable()
        {
            base.Enable();

            if (vc.powertrain.engine.powerModifiers.All(p => p != NOSPowerModifier))
            {
                vc.powertrain.engine.powerModifiers.Add(NOSPowerModifier);
            }
        }


        public override void Disable()
        {
            base.Disable();

            vc.powertrain.engine.powerModifiers.RemoveAll(p => p == NOSPowerModifier);
        }


        public float NOSPowerModifier()
        {
            if (!vc.input.Boost || !Active || vc.powertrain.transmission.Ratio <= 0 && disableInReverse
                || vc.powertrain.engine.ThrottlePosition < 0.1f && disableOffThrottle)
            {
                return 1f;
            }

            charge -= flow * vc.fixedDeltaTime;
            charge = charge < 0 ? 0 : charge > capacity ? capacity : charge;

            if (charge <= 0)
            {
                return 1f;
            }

            if (vc.effectsManager.exhaustFlash.Active)
            {
                vc.effectsManager.exhaustFlash.Flash(false);
            }

            return powerCoefficient;
        }

        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            nosSoundComponent.SetDefaults(vc);
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.NOS
{
    [CustomPropertyDrawer(typeof(NOSModule))]
    public partial class NOSModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("capacity");
            drawer.Field("charge");
            drawer.Field("flow");
            drawer.Field("powerCoefficient");
            drawer.Field("exhaustEmissionCoefficient");
            drawer.Field("engineVolumeCoefficient");
            drawer.Field("disableOffThrottle");
            drawer.Field("disableInReverse");
            drawer.Property("nosSoundComponent");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
