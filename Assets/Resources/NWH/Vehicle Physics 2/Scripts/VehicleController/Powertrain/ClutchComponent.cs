using System;
using NWH.Common.Utility;
using UnityEngine;
using UnityEngine.Serialization;
using NWH.Common.Vehicles;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Powertrain
{
    [Serializable]
    public partial class ClutchComponent : PowertrainComponent
    {
        public enum CalculationType { Realistic, Arcade }

        /// <summary>
        ///     Determines which calculation will be used for simulating the clutch. Use Realistic to be able to stall
        ///     the engine and vice versa. Arcade clutch will also result in more power to the wheels.
        /// </summary>
        [UnityEngine.Tooltip("    Determines which calculation will be used for simulating the clutch. Use Realistic to be able to stall\r\n    the engine and vice versa. Arcade clutch will also result in more power to the wheels.")]
        [ShowInSettings("Type")]
        [ShowInTelemetry]
        public CalculationType calculationType = CalculationType.Realistic;

        /// <summary>
        ///     RPM at which automatic clutch will try to engage.
        /// </summary>
        [Tooltip("    RPM at which automatic clutch will try to engage.")]
        [FormerlySerializedAs("baseEngagementRPM")]
        [ShowInTelemetry]
        [ShowInSettings("Engagement RPM", 900, 2000, 100)]
        public float engagementRPM = 1200f;

        /// <summary>
        ///     Clutch engagement in range [0,1] where 1 is fully engaged clutch.
        ///     Affected by Slip Torque field as the clutch can transfer [clutchEngagement * slipTorque] Nm
        ///     meaning that higher value of slipTorque will result in more sensitive clutch.
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "Clutch engagement in range [0,1] where 1 is fully engaged clutch.\r\nAffected by Slip Torque field as the clutch can transfer [clutchEngagement * slipTorque] Nm\r\nmeaning that higher value of slipTorque will result in more sensitive clutch.")]
        [ShowInTelemetry]
        public float clutchEngagement;

        /// <summary>
        /// Curve representing pedal travel vs. clutch engagement. Should start at 0,0 and end at 1,1.
        /// </summary>
        [UnityEngine.Tooltip("Curve representing pedal travel vs. clutch engagement. Should start at 0,0 and end at 1,1.")]
        public AnimationCurve clutchEngagementCurve = new AnimationCurve();

        /// <summary>
        ///     Is the clutch automatic? If true any input set manually will be overridden by the result given by PID controller
        ///     based on the difference between engine and clutch RPM.
        /// </summary>      
        [Tooltip(
            "Is the clutch automatic? If true any input set manually will be overridden by the result given by PID controller\r\nbased on the difference between engine and clutch RPM.")]
        [ShowInSettings("Is Automatic")]
        public bool isAutomatic = true;

        /// <summary>
        /// The RPM range in which the clutch will go from disengaged to engaged and vice versa. 
        /// E.g. if set to 400 and engagementRPM is 1000, 1000 will mean clutch is fully disengaged and
        /// 1400 fully engaged. Setting it too low might cause clutch to hunt/oscillate.
        /// </summary>
        [ShowInSettings("Engagement Range", 200f, 1000f, 100f)]
        public float engagementRange = 400f;

        /// <summary>
        ///     Torque at which the clutch will slip / maximum torque that the clutch can transfer.
        ///     This value also affects clutch engagement as higher slip value will result in clutch
        ///     that grabs higher up / sooner. Too high slip torque value combined with low inertia of
        ///     powertrain components might cause instability in powertrain solver.
        /// </summary>
        [SerializeField]
        [Tooltip(
            "Torque at which the clutch will slip / maximum torque that the clutch can transfer.\r\nThis value also affects clutch engagement as higher slip value will result in clutch\r\nthat grabs higher up / sooner. Too high slip torque value combined with low inertia of\r\npowertrain components might cause instability in powertrain solver.")]
        [ShowInSettings("Slip Torque", 10f, 5000f, 100f)]
        public float slipTorque = 500f;

        /// <summary>
        /// Amount of torque that will be passed through clutch even when completely disengaged
        /// to emulate torque converter creep on automatic transmissions.
        /// Should be higher than rolling resistance of the wheels to get the vehicle rolling.
        /// </summary>
        [Tooltip("Amount of torque that will be passed through clutch even when completely disengaged to emulate torque converter creep on automatic transmissions." +
                 "Should be higher than rolling resistance of the wheels to get the vehicle rolling.")]
        [ShowInSettings("Creep Torque", 0f, 100f, 10f)]
        public float creepTorque = 0;


        public bool IsFullyEngaged
        {
            get => clutchEngagement > 0.98f;
        }

        public override void OnPrePhysicsStep(float dt)
        {
            base.OnPrePhysicsStep(dt);

            if (!isAutomatic)
            {
                clutchEngagement = vc.input.Clutch;
            }
        }


        public override void Initialize(VehicleController vc)
        {
            base.Initialize(vc);

            if (clutchEngagementCurve.keys.Length < 2)
            {
                SetDefaultClutchEngagementCurve();
            }
        }


        public override void OnDisable()
        {
            base.OnDisable();

            clutchEngagement = 0;
        }


        public override void SetDefaults(VehicleController vc)
        {
            inertia = 0.02f;
            slipTorque = vc.powertrain.engine.PeakTorque * 1.5f;
            SetDefaultClutchEngagementCurve();

            SetOutput(vc.powertrain.transmission);
        }


        private void SetDefaultClutchEngagementCurve()
        {
            clutchEngagementCurve = new AnimationCurve(
                new Keyframe[2]
                {
                    new Keyframe(0, 0),
                    new Keyframe(1, 1)
                }); // Linear clutch engagement
        }


        public override void Validate(VehicleController vc)
        {
            base.Validate(vc);

            Debug.Assert(!string.IsNullOrEmpty(outputASelector.name),
                         "Clutch is not connected to anything. Go to Powertrain > Clutch and set the output.");
        }


        public override float QueryAngularVelocity(float inputAngularVelocity, float dt)
        {
            _angularVelocity = inputAngularVelocity;

            if (_outputAIsNull)
            {
                return inputAngularVelocity;
            }

            // Adjust engagement
            if (isAutomatic)
            {
                // Increase engagement point on hill
                float finalEngagementRPM = engagementRPM + engagementRPM * Mathf.Clamp(Vector3.Dot(vc.vehicleTransform.forward, Vector3.up), 0f, 0.2f);
                clutchEngagement = Mathf.Clamp01((RPM - finalEngagementRPM) / engagementRange);
            }

            // Solver uses velocity based approach which is not ideal for clutch simulation
            float Wout = outputA.QueryAngularVelocity(inputAngularVelocity * clutchEngagement, dt) * clutchEngagement;
            float Win = inputAngularVelocity * (1f - clutchEngagement);
            float W = Wout + Win;
            return W;
        }


        public override float QueryInertia()
        {
            if (_outputAIsNull)
            {
                return inertia;
            }

            float I = inertia + outputA.QueryInertia() * clutchEngagement;
            return I;
        }


        public override float ForwardStep(float torque, float inertiaSum, float dt)
        {
            _torque = torque;

            if (_outputAIsNull)
            {
                return torque;
            }

            if (calculationType == CalculationType.Realistic)
            {
                return CalculateTorqueRealistic(torque, inertiaSum, dt);
            }
            else
            {
                return CalculateTorqueArcade(torque, inertiaSum, dt);
            }
        }


        private float CalculateTorqueArcade(float torque, float inertiaSum, float dt)
        {
            float clutchEngagmentCurveValue = clutchEngagementCurve.Evaluate(clutchEngagement);
            torque = torque > slipTorque ? slipTorque : torque < -slipTorque ? -slipTorque : torque;

            float returnTorque =
                outputA.ForwardStep(torque * (1f - (1f - Mathf.Pow(clutchEngagmentCurveValue, 0.3f))),
                                    inertiaSum * clutchEngagement + inertia, dt) * clutchEngagmentCurveValue;
            returnTorque = returnTorque > slipTorque ? slipTorque :
                           returnTorque < -slipTorque ? -slipTorque : returnTorque;
            return returnTorque;
        }


        private float CalculateTorqueRealistic(float torque, float inertiaSum, float dt)
        {
            float clutchEngagmentCurveValue = clutchEngagementCurve.Evaluate(clutchEngagement);

            torque = torque > slipTorque ? slipTorque : torque < -slipTorque ? -slipTorque : torque;

            float forwardTorque = torque * clutchEngagmentCurveValue;
            float forwardInertia = inertiaSum * clutchEngagement + inertia;

            // Apply creep torque to forward torque
            if (creepTorque != 0)
            {
                forwardTorque = forwardTorque <= creepTorque && forwardTorque >= -creepTorque // Check if torque lower than creep torque
                    ? forwardTorque > 0 ? -creepTorque : creepTorque // Apply creep torque with the sign of the input torque
                    : forwardTorque; // If torque above creep torque, ignore
            }

            float returnTorque = outputA.ForwardStep(forwardTorque, forwardInertia, dt); // Forward maxStep the powertrain
            returnTorque *= clutchEngagmentCurveValue;

            returnTorque = returnTorque > slipTorque
                ? slipTorque  // Return torque is higher than slip torque, slip the clutch
                : returnTorque < -slipTorque
                    ? -slipTorque // Return torque is lower than negative slip torque, slip the clutch
                    : returnTorque; // Return torque is within slip bounds, do nothing

            return returnTorque;
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Powertrain
{
    [CustomPropertyDrawer(typeof(ClutchComponent))]
    public partial class ClutchComponentDrawer : PowertrainComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            DrawCommonProperties();

            drawer.BeginSubsection("Automatic Clutch");
            if (drawer.Field("isAutomatic").boolValue)
            {
                drawer.Field("engagementRPM");
                drawer.Field("engagementRange");
                drawer.EndSubsection();
            }
            drawer.EndSubsection();

            drawer.BeginSubsection("General");
            drawer.Field("calculationType");
            bool isAutomatic = drawer.FindProperty("isAutomatic").boolValue;
            drawer.Field("clutchEngagement", false);
            if (isAutomatic)
            {
                drawer.Info(
                    "Clutch engagement is being set automatically.");
            }
            else
            {
                drawer.Info(
                    "Clutch engagement is being set through user input. Check input settings for 'Clutch' axis.");
            }

            drawer.Field("clutchEngagementCurve");
            drawer.Field("slipTorque", true, "Nm");
            drawer.Field("creepTorque", true, "Nm");
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
