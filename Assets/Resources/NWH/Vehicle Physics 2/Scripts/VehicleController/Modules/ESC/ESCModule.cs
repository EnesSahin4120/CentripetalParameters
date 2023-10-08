using System;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.ESC
{
    /// <summary>
    ///     Electronic Stability Control (ESC) module.
    ///     Applies braking on individual wheels to try and stabilize the vehicle when the vehicle velocity and vehicle
    ///     direction do not match.
    /// </summary>
    [Serializable]
    public partial class ESCModule : VehicleModule
    {
        /// <summary>
        ///     Intensity of stability control.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Intensity of stability control.")]
        public float intensity = 0.4f;

        /// <summary>
        ///     ESC will not work below this speed.
        ///     Setting this to a too low value might cause vehicle to be hard to steer at very low speeds.
        /// </summary>
        [Tooltip(
            "ESC will not work below this speed.\r\nSetting this to a too low value might cause vehicle to be hard to steer at very low speeds.")]
        public float lowerSpeedThreshold = 4f;


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            // Prevent ESC from working in reverse and at low speeds
            if (vc.LocalForwardVelocity < lowerSpeedThreshold)
            {
                return;
            }

            float angle = Vector3.SignedAngle(vc.vehicleRigidbody.velocity, vc.vehicleTransform.forward, vc.vehicleTransform.up);
            angle -= vc.steering.angle * 0.5f;
            float absAngle = angle < 0 ? -angle : angle;

            if (vc.powertrain.engine.revLimiterActive || absAngle < 2f)
            {
                return;
            }

            foreach (WheelComponent wheelComponent in vc.Wheels)
            {
                if (!wheelComponent.wheelUAPI.IsGrounded)
                {
                    continue;
                }

                float additionalBrakeTorque = -angle * Mathf.Sign(wheelComponent.wheelUAPI.transform.position.x) * 50f * intensity;
                wheelComponent.AddBrakeTorque(additionalBrakeTorque);
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.ESC
{
    [CustomPropertyDrawer(typeof(ESCModule))]
    public partial class ESCModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Info("ECS only works with 4-wheel vehicles.");
            drawer.Field("intensity");
            drawer.Field("lowerSpeedThreshold");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
