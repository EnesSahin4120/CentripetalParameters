using System;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.ABS
{
    /// <summary>
    ///     Anti-lock Braking System module.
    ///     Prevents wheels from locking up by reducing brake torque when slip reaches too high value.
    /// </summary>
    [Serializable]
    public partial class ABSModule : VehicleModule
    {
        /// <summary>
        ///     Called each frame while ABS is a active.
        /// </summary>
        [Tooltip("    Called each frame while ABS is a active.")]
        public UnityEvent ABSActive = new UnityEvent();

        /// <summary>
        ///     Is ABS currently active?
        /// </summary>
        [Tooltip("    Is ABS currently active?")]
        public bool active;

        /// <summary>
        ///     ABS will not work below this speed.
        /// </summary>
        [Tooltip("    ABS will not work below this speed.")]
        public float lowerSpeedThreshold = 1f;

        /// <summary>
        ///     Longitudinal slip required for ABS to trigger. Larger value means less sensitive ABS.
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "Longitudinal slip required for ABS to trigger.")]
        public float slipThreshold = 0.1f;


        public override void Start(VehicleController vc)
        {
            base.Start(vc);

            if (ABSActive == null)
            {
                ABSActive = new UnityEvent();
            }
        }


        public override void Enable()
        {
            base.Enable();

            bool all = true;
            foreach (Brakes.BrakeTorqueModifier x in vc.brakes.brakeTorqueModifiers)
            {
                if (x == BrakeTorqueModifier)
                {
                    all = false;
                    break;
                }
            }

            if (vc != null && all)
            {
                vc.brakes.brakeTorqueModifiers.Add(BrakeTorqueModifier);
            }
        }


        public override void Disable()
        {
            base.Disable();
            active = false;

            if (vc != null)
            {
                vc.brakes.brakeTorqueModifiers.RemoveAll(p => p == BrakeTorqueModifier);
            }
        }


        public float BrakeTorqueModifier()
        {
            if (!Active)
            {
                return 1f;
            }

            active = false;
            slipThreshold = slipThreshold < 0 ? 0 : slipThreshold > 1 ? 1 : slipThreshold;

            // Prevent ABS from working in reverse and at low speeds
            if (vc.LocalForwardVelocity < lowerSpeedThreshold)
            {
                return 1f;
            }

            if (vc.brakes.Active && !vc.powertrain.engine.revLimiterActive && vc.input.Handbrake < 0.1f)
            {
                for (int index = 0; index < vc.Wheels.Count; index++)
                {
                    WheelComponent wheelComponent = vc.Wheels[index];
                    if (!wheelComponent.wheelUAPI.IsGrounded)
                    {
                        continue;
                    }

                    float longSlip = wheelComponent.wheelUAPI.LongitudinalSlip;
                    if (longSlip > 0 && longSlip > slipThreshold)
                    {
                        active = true;
                        ABSActive.Invoke();
                        return 0.01f;
                    }
                }
            }

            return 1f;
        }
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Modules.ABS
{
    [CustomPropertyDrawer(typeof(ABSModule))]
    public partial class ABSModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("slipThreshold");
            drawer.Field("lowerSpeedThreshold", true, "m/s");
            drawer.Field("active", false);
            drawer.Field("ABSActive");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
