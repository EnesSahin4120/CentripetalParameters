using System;
using NWH.VehiclePhysics2.Powertrain;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using UnityEngine;
using NWH.Common.Vehicles;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Controls vehicle's steering and steering geometry.
    /// </summary>
    [Serializable]
    public partial class Steering : VehicleComponent
    {
        /// <summary>
        ///     Only used if limitSteeringRate is true. Will limit wheels so that they can only steer up to the set degree
        ///     limit per second. E.g. 60 degrees per second will mean that the wheels that have 30 degree steer angle will
        ///     take 1 second to steer from full left to full right.
        /// </summary>
        [Tooltip(
            "Only used if limitSteeringRate is true.Will limit wheels so that they can only steer up to the set degree" +
            "limit per second. E.g. 60 degrees per second will mean that the wheels that have 30 degree steer angle will" +
            "take 1 second to steer from full left to full right.")]
        [ShowInSettings("deg/s Limit", 50f, 500f, 10f)]
        public float degreesPerSecondLimit = 180f;

        /// <summary>
        ///     If true direct steering input will be used, without any modification.
        /// </summary>
        [Tooltip("    If true direct steering input will be used, without any modification.")]
        [ShowInSettings("Raw Input")]
        public bool useRawInput;

        public AnimationCurve linearity = new AnimationCurve(
            new Keyframe(0, 0, 1, 1),
            new Keyframe(1, 1, 1, 1)
        );

        /// <summary>
        ///     Maximum steering angle at the wheels.
        /// </summary>
        [Range(0f, 90f)]
        [Tooltip("    Maximum steering angle at the wheels.")]
        [ShowInSettings("Max. Steer Angle", 5f, 50f, 5f)]
        public float maximumSteerAngle = 25f;

        /// <summary>
        ///     Should wheels return to neutral position when there is no input?
        /// </summary>
        [Tooltip("    Should wheels return to neutral position when there is no input?")]
        [ShowInSettings("Return to Center")]
        public bool returnToCenter = true;

        /// <summary>
        ///     Curve that shows how the steering angle behaves at certain speed.
        ///     X axis represents velocity in range 0 to 100m/s (normalized to 0,1).
        ///     Y axis represents 0 to maximumSteerAngle (normalized to 0,1).
        /// </summary>
        [Tooltip(
            "Curve that shows how the steering angle behaves at certain speed.\r\nX axis represents velocity in range 0 to 100m/s (normalized to 0,1).\r\nY axis represents 0 to maximumSteerAngle (normalized to 0,1).")]
        public AnimationCurve speedSensitiveSteeringCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.3f, 0.6f, 0f, -0.6f),
            new Keyframe(1f, 0.1f, 0.5f, 0f)
        );

        public AnimationCurve speedSensitiveSmoothingCurve = new AnimationCurve(
           new Keyframe(0f, 0.05f),
           new Keyframe(1f, 0.15f)
       );

        /// <summary>
        ///     Steering wheel transform that will be rotated when steering. Optional.
        /// </summary>
        [Tooltip("    Steering wheel transform that will be rotated when steering. Optional.")]
        public Transform steeringWheel;

        /// <summary>
        ///     Steer angle will be multiplied by this value to get steering wheel angle. Ignored if steering wheel is null.
        ///     If you want the steering wheel to rotate in opposite direction use negative value.
        /// </summary>
        [Tooltip(
            "Steer angle will be multiplied by this value to get steering wheel angle. Ignored if steering wheel is null.\r\nIf you want the steering wheel to rotate in opposite direction use negative value.")]
        public float steeringWheelTurnRatio = 5f;

        private Vector3 _initialSteeringWheelRotation;
        private float _steerVelocity;
        private float _targetAngle;

        /// <summary>
        ///     Current steer angle.
        /// </summary>
        [UnityEngine.Tooltip("    Current steer angle.")]
        [ShowInTelemetry]
        public float angle;

        /// <summary>
        ///     angle added to the user set angle, used mostly for motorcycle balancing.
        ///     To add angle to the current steer angle use this instead of angle, since this goes around smoothing and clamping.
        /// </summary>
        [UnityEngine.Tooltip("    angle added to the user set angle, used mostly for motorcycle balancing.\r\n    To add angle to the current steer angle use this instead of angle, since this goes around smoothing and clamping.")]
        public float externallyAddedAngle;

        public override void Initialize()
        {
            if (steeringWheel != null)
            {
                _initialSteeringWheelRotation = steeringWheel.transform.localRotation.eulerAngles;
            }

            vc.wheelbase = CalculateWheelbase();

            base.Initialize();
        }


        public override void Start(VehicleController vc)
        {
            base.Start(vc);
            _targetAngle = 0;
            _steerVelocity = 0;
        }


        public override void FixedUpdate()
        {
            if (!Active)
            {
                return;
            }

            CalculateSteerAngles();
            VisualUpdate();
        }


        public virtual void CalculateSteerAngles()
        {
            float horizontalInput = vc.input.Steering;
            float smoothing = speedSensitiveSmoothingCurve.Evaluate(vc.Speed / 50f);
            if (!useRawInput && !returnToCenter && horizontalInput > -0.04f && horizontalInput < 0.04f)
            {
                return;
            }

            if (useRawInput)
            {
                angle = horizontalInput * maximumSteerAngle;
            }
            else
            {
                float absHorizontalInput = horizontalInput < 0 ? -horizontalInput : horizontalInput;
                float horizontalInputSign = horizontalInput < 0 ? -1 : 1;
                float maxAngle = speedSensitiveSteeringCurve.Evaluate(vc.Speed / 50f) * maximumSteerAngle;
                float inputAngle = maxAngle * linearity.Evaluate(absHorizontalInput) * horizontalInputSign;
                _targetAngle = Mathf.SmoothDamp(_targetAngle, inputAngle, ref _steerVelocity, smoothing);
                angle = Mathf.MoveTowards(angle, _targetAngle, degreesPerSecondLimit * vc.fixedDeltaTime);
            }

            foreach (WheelGroup wheelGroup in vc.WheelGroups)
            {
                float axleSteerAngle = (angle + externallyAddedAngle) * wheelGroup.steerCoefficient;

                // Apply Ackermann angle
                if (wheelGroup.Wheels.Count == 2 && vc.wheelbase > 0.001f && wheelGroup.addAckerman)
                {
                    float axleAngleRad = axleSteerAngle * Mathf.Deg2Rad;
                    float sinAxleAngle = Mathf.Sin(axleAngleRad);
                    float cosAxleAngle = Mathf.Cos(axleAngleRad);

                    float angleInnerRad = Mathf.Atan(4f * wheelGroup.trackWidth * sinAxleAngle /
                                                (2f * vc.wheelbase * cosAxleAngle - wheelGroup.trackWidth * sinAxleAngle));

                    float angleOuterRad = Mathf.Atan(4f * wheelGroup.trackWidth * sinAxleAngle /
                                                (2f * vc.wheelbase * cosAxleAngle + wheelGroup.trackWidth * sinAxleAngle));

                    if (axleSteerAngle < 0)
                    {
                        wheelGroup.RightWheel.wheelUAPI.SteerAngle = angleInnerRad * Mathf.Rad2Deg;
                        wheelGroup.LeftWheel.wheelUAPI.SteerAngle = angleOuterRad * Mathf.Rad2Deg;
                    }
                    else
                    {
                        wheelGroup.LeftWheel.wheelUAPI.SteerAngle = angleOuterRad * Mathf.Rad2Deg;
                        wheelGroup.RightWheel.wheelUAPI.SteerAngle = angleInnerRad * Mathf.Rad2Deg;
                    }
                }
                else
                {
                    foreach (WheelComponent wheel in wheelGroup.Wheels)
                    {
                        wheel.wheelUAPI.SteerAngle = axleSteerAngle;
                    }
                }
            }
        }


        public float CalculateWheelbase()
        {
            // Calculate wheelbase
            float wheelbase = -1;
            int wheelCount = vc.powertrain.wheels.Count;
            if (wheelCount == 4)
            {
                wheelbase = Vector3.Distance(
                    vc.powertrain.wheels[0].wheelUAPI.transform.position,
                    vc.powertrain.wheels[2].wheelUAPI.transform.position);
            }
            else if (wheelCount == 2)
            {
                wheelbase = Vector3.Distance(
                    vc.powertrain.wheels[0].wheelUAPI.transform.position,
                    vc.powertrain.wheels[1].wheelUAPI.transform.position);
            }
            return wheelbase;
        }


        public virtual void VisualUpdate()
        {
            // Adjust steering wheel object if it exists
            if (steeringWheel != null)
            {
                float wheelAngle = angle * steeringWheelTurnRatio;
                steeringWheel.transform.localRotation = Quaternion.Euler(_initialSteeringWheelRotation);
                steeringWheel.transform.Rotate(Vector3.forward, wheelAngle);
            }
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            speedSensitiveSteeringCurve = new AnimationCurve(
                new Keyframe(0f, 1f, 0f, 0f),
                new Keyframe(0.3f, 0.4f, -0.6f, -0.6f),
                new Keyframe(1f, 0.2f, -0.1f, 0.1f)
            );

            linearity = new AnimationCurve(
                new Keyframe(0, 0, 1, 1),
                new Keyframe(1, 1, 1, 1));
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Property drawer for Steering.
    /// </summary>
    [CustomPropertyDrawer(typeof(Steering))]
    public partial class SteeringDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.BeginSubsection("Steer angle");
            drawer.Field("maximumSteerAngle");
            if (!drawer.Field("useRawInput").boolValue)
            {
                drawer.Space(10);
                drawer.Field("speedSensitiveSteeringCurve");
                drawer.Field("speedSensitiveSmoothingCurve");
                drawer.Info("X-axis is normalized to 0 to 1, representing speed of 0 to 50 m/s.");
                drawer.Space(10);
                drawer.Field("linearity");
                drawer.Field("degreesPerSecondLimit");
                drawer.Field("returnToCenter");
            }

            drawer.EndSubsection();

            drawer.BeginSubsection("Animation");
            drawer.Field("steeringWheel");
            drawer.Field("steeringWheelTurnRatio");
            drawer.Info("Use negative turn ratio to turn the wheel in opposite direction if needed.");
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
