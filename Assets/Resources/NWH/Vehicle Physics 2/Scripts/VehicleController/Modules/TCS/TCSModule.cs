using System;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.TCS
{
    /// <summary>
    ///     Traction Control System (TCS) module. Reduces engine throttle when excessive slip is present.
    /// </summary>
    [Serializable]
    public partial class TCSModule : VehicleModule
    {
        public bool active;

        /// <summary>
        ///     Speed under which TCS will not work.
        /// </summary>
        [Tooltip("    Speed under which TCS will not work.")]
        public float lowerSpeedThreshold = 2f;

        /// <summary>
        ///     Longitudinal slip threshold at which TCS will activate.
        /// </summary>
        [Range(0f, 1f)]
        [Tooltip("    Longitudinal slip threshold at which TCS will activate.")]
        public float slipThreshold = 0.1f;

        /// <summary>
        ///     Called each frame while TCS is active.
        /// </summary>
        [Tooltip("    Called each frame while TCS is active.")]
        public UnityEvent TCSActive = new UnityEvent();


        public override void Start(VehicleController vc)
        {
            base.Start(vc);
            if (TCSActive == null)
            {
                TCSActive = new UnityEvent();
            }
        }


        public override void Enable()
        {
            base.Enable();

            if (vc != null)
            {
                bool all = true;
                foreach (EngineComponent.PowerModifier p in vc.powertrain.engine.powerModifiers)
                {
                    if (p == TCSPowerLimiter)
                    {
                        all = false;
                        break;
                    }
                }

                if (all)
                {
                    vc.powertrain.engine.powerModifiers.Add(TCSPowerLimiter);
                }
            }
        }


        public override void Disable()
        {
            base.Disable();
            active = false;

            if (vc != null)
            {
                vc.powertrain.engine.powerModifiers.RemoveAll(p => p == TCSPowerLimiter);
            }
        }


        public float TCSPowerLimiter()
        {
            active = false;

            if (!Active)
            {
                return 1f;
            }

            if (vc.Speed > lowerSpeedThreshold)
            {
                foreach (WheelComponent wheelComponent in vc.Wheels)
                {
                    if (!wheelComponent.wheelUAPI.IsGrounded || vc.powertrain.transmission.IsShifting)
                    {
                        continue;
                    }

                    float longSlip = wheelComponent.wheelUAPI.LongitudinalSlip;
                    if (longSlip < 0 && longSlip < -slipThreshold)
                    {
                        active = true;
                        TCSActive.Invoke();
                        return 0f;
                    }
                }
            }

            return 1f;
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.TCS
{
    [CustomPropertyDrawer(typeof(TCSModule))]
    public partial class TCSModuleDrawer : ComponentNUIPropertyDrawer
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
            drawer.Field("TCSActive");

            drawer.EndProperty();
            return true;
        }
    }
}
#endif
