using System;
using NWH.Common.Utility;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.Modules.MotorcycleModule
{
    /// <summary>
    ///     Module that adds motorcycle balance and steering to VehicleController.
    /// </summary>
    [Serializable]
    public partial class MotorcycleModule : VehicleModule
    {
        // Lean and turning

        /// <summary>
        /// Maximum angle delta in [deg] per [s] for given speed [m/s].
        /// </summary>
        [UnityEngine.Tooltip("Maximum angle delta in [deg] per [s] for given speed [m/s].")]
        public AnimationCurve leanAngleMaxDelta = new AnimationCurve(
                new Keyframe[2]
                {
                    new Keyframe(0f, 220f),
                    new Keyframe(90f, 100f)
                });

        /// <summary>
        /// Maximum lean angle [deg] for given speed [m/s].
        /// </summary>
        [UnityEngine.Tooltip("Maximum lean angle [deg] for given speed [m/s].")]
        public AnimationCurve maxLeanAngle = new AnimationCurve(
                new Keyframe[2]
                {
                    new Keyframe(0f, 33f),
                    new Keyframe(90f, 33f)
                });

        /// <summary>
        /// Lean angle addition given the lateral tire slip.
        /// Allows the motorcycle to lean and slide sideways when drifting,
        /// instead of highsiding.
        /// </summary>
        [UnityEngine.Tooltip("Lean angle addition given the lateral tire slip.\r\nAllows the motorcycle to lean and slide sideways when drifting,\r\ninstead of highsiding.")]
        public float leanAngleSlipCoefficient = -30f;

        /// <summary>
        /// Maximum torque the lean controller can apply to the Rigidbody.
        /// Too small value will result in lack of lean control on the vehicle
        /// in extreme cases, but can be more realistic as the motorcycle will be able to
        /// fall over, highside, etc.
        /// </summary>
        [UnityEngine.Tooltip("Maximum torque the lean controller can apply to the Rigidbody.\r\nToo small value will result in lack of lean control on the vehicle\r\nin extreme cases, but can be more realistic as the motorcycle will be able to\r\nfall over, highside, etc.")]
        public float maxLeanTorque = 20000f;


        // PID

        /// <summary>
        /// Lean PID controller proportional gain.
        /// </summary>
        [UnityEngine.Tooltip("Lean PID controller proportional gain.")]
        public float gainProportional = 3f;

        /// <summary>
        /// Lean PID controller integral gain.
        /// </summary>
        [UnityEngine.Tooltip("Lean PID controller integral gain.")]
        public float gainIntegral = 2f;

        /// <summary>
        /// Lean PID controller derivative gain.
        /// </summary>
        [UnityEngine.Tooltip("Lean PID controller derivative gain.")]
        public float gainDerivative = 0.5f;

        /// <summary>
        /// Lean PID controller proportional gain.
        /// </summary>
        [UnityEngine.Tooltip("Lean PID controller proportional gain.")]
        public float leanPIDCoefficient = 50f;

        // Animation

        /// <summary>
        /// Transform representing the lower forks.
        /// </summary>
        [UnityEngine.Tooltip("Transform representing the lower forks.")]
        public Transform forksTransform;

        /// <summary>
        /// Transform representing the upper forks and handlebars.
        /// </summary>
        [UnityEngine.Tooltip("Transform representing the upper forks and handlebars.")]
        public Transform handlebarsTransform;

        /// <summary>
        /// Transform representing the rear swingarm.
        /// </summary>
        [UnityEngine.Tooltip("Transform representing the rear swingarm.")]
        public Transform swingarmTransform;


        private float _leanTorque;
        private float _turningRadius;
        private float _leanAngleCurrent;
        private float _leanAngleTarget;
        private float _leanAngleTargetSmoothed;
        private float _leanAngleSlipContribution;
        private float _steerAngleTarget;
        private float _steeringLeanAngleReference;
        private Transform _transform;
        private Rigidbody _rb;
        private PIDController _leanPIDController;
        private float _gravity;
        private float _speed;
        private float _absSpeed;
        private WheelComponent _frontWheel;
        private WheelComponent _rearWheel;

        private Quaternion _forkInitRotation;
        private Quaternion _handlebarInitRotation;


        /// <summary>
        /// Is the front wheel on the ground?
        /// </summary>
        public bool FrontWheelGrounded
        {
            get { return _frontWheel.wheelUAPI.IsGrounded; }
        }

        /// <summary>
        /// Is the rear wheel on the ground?
        /// </summary>
        public bool RearWheelGrounded
        {
            get { return _rearWheel.wheelUAPI.IsGrounded; }
        }

        /// <summary>
        /// Are both wheels on the ground?
        /// </summary>
        public bool IsGrounded
        {
            get { return FrontWheelGrounded && RearWheelGrounded; }
        }

        /// <summary>
        /// Is the motorcycle driving on rear wheel only?
        /// </summary>
        public bool IsWheelie
        {
            get { return !FrontWheelGrounded && RearWheelGrounded; }
        }

        /// <summary>
        /// Is the motorcycle driving on the front wheel only?
        /// </summary>
        public bool IsStoppie
        {
            get { return !RearWheelGrounded && FrontWheelGrounded; }
        }

        public override void Initialize()
        {
            Debug.Assert(vc.Wheels.Count == 2,
                "Motorcycle has more than two wheels. Exactly two wheels required in order: front, back.");

            _rb = vc.vehicleRigidbody;
            _transform = vc.vehicleTransform;
            _leanPIDController = new PIDController(gainProportional, gainIntegral, gainDerivative, -maxLeanTorque, maxLeanTorque);
            _frontWheel = vc.Wheels[0];
            _rearWheel = vc.Wheels[1];

            if (forksTransform != null) _forkInitRotation = forksTransform.localRotation;
            if (handlebarsTransform != null) _handlebarInitRotation = handlebarsTransform.localRotation;

            base.Initialize();
        }


        public override void FixedUpdate()
        {
            if (!Active)
            {
                return;
            }

            if (swingarmTransform != null)
            {
                swingarmTransform.LookAt(_rearWheel.wheelUAPI.WheelPosition, _transform.up);
            }

            if (handlebarsTransform != null)
            {
                handlebarsTransform.localRotation = _handlebarInitRotation * Quaternion.AngleAxis(_frontWheel.wheelUAPI.SteerAngle, Vector3.up);
            }

            if (forksTransform != null)
            {
                forksTransform.position = _frontWheel.wheelUAPI.WheelPosition;
                forksTransform.localRotation = _forkInitRotation * Quaternion.AngleAxis(_frontWheel.wheelUAPI.SteerAngle, Vector3.up);
            }

            _gravity = -Physics.gravity.y;
            _speed = vc.Speed;
            _absSpeed = Mathf.Abs(_speed);

            // Get current lean angle
            if (Vector3.Dot(_transform.up, Vector3.up) > 0.2f)
            {
                _leanAngleCurrent = Vector3.SignedAngle(_transform.up, Vector3.up, _transform.forward);
            }
            else
            {
                _leanAngleCurrent = _leanAngleTarget;
                return;
            }

            // Calculate ideal target lean angle
            _leanAngleTarget = vc.input.Steering * maxLeanAngle.Evaluate(_absSpeed);
            _leanAngleSlipContribution = Mathf.Clamp(_rearWheel.wheelUAPI.LateralSlip, -1f, 1f);
            _leanAngleTarget += _leanAngleSlipContribution * leanAngleSlipCoefficient;

            // Calculate smooth lean angle
            float leanAngleMaxDeltaCurrent = leanAngleMaxDelta.Evaluate(_absSpeed);

            // Ease in lean angle as lean at 0 speed is not good.
            //leanAngleTargetSmoothed = Mathf.Sign(leanAngleTargetSmoothed) * Mathf.Lerp(0f, Mathf.Abs(leanAngleTargetSmoothed), _absSpeed * 0.5f);

            _leanAngleTargetSmoothed =
                Mathf.MoveTowardsAngle(_leanAngleTargetSmoothed, _leanAngleTarget, leanAngleMaxDeltaCurrent * Time.fixedDeltaTime);

            // Calculate steer angle
            _steeringLeanAngleReference =
                Mathf.MoveTowardsAngle(
                    _steeringLeanAngleReference,
                     Mathf.Clamp01(1f - _absSpeed * 0.12f) * _leanAngleTarget + Mathf.Clamp01(_absSpeed * 0.12f) * _leanAngleCurrent,
                    leanAngleMaxDeltaCurrent);
            _steerAngleTarget = GetSteerAngleForLeanAngle(_steeringLeanAngleReference, _speed, vc.wheelbase);
            _steerAngleTarget = Mathf.Clamp(_steerAngleTarget, -vc.steering.maximumSteerAngle, vc.steering.maximumSteerAngle);
            vc.input.Steering = vc.steering.maximumSteerAngle == 0 ? _steerAngleTarget : _steerAngleTarget / vc.steering.maximumSteerAngle;

            // Generate lean PID controller values
            _leanPIDController.GainProportional = gainProportional * leanPIDCoefficient;
            _leanPIDController.GainIntegral = gainIntegral * leanPIDCoefficient;
            _leanPIDController.GainDerivative = gainDerivative * leanPIDCoefficient;
            _leanPIDController.maxValue = maxLeanTorque;

            _leanPIDController.ProcessVariable = _leanAngleCurrent;
            _leanPIDController.SetPoint = _leanAngleTargetSmoothed;
            _leanTorque = -_leanPIDController.ControlVariable(Time.fixedDeltaTime);

            // Apply lean torque
            _rb.AddForceAtPosition(-_transform.right * _leanTorque, vc.vehicleRigidbody.worldCenterOfMass + _transform.up);
        }


        /// <summary>
        /// Calculates neutral steering angle of the front wheel.
        /// </summary>
        /// <param name="wheelbase">Wheelbase in meters.</param>
        /// <param name="leanAngle">Lean angle in degrees.</param>
        /// <param name="casterAngle">Caster angle in degrees.</param>
        /// <param name="speed">Speed in m/s.</param>
        /// <returns>Steer angle in degrees.</returns>
        private float GetNeutralSteerAngle(float wheelbase, float leanAngle, float casterAngle, float speed)
        {
            if (leanAngle > -0.0001f && leanAngle < 0.0001f)
            {
                return 0f;
            }

            float leanAngleRad = leanAngle * Mathf.Deg2Rad;
            float cosLeanAngle = Mathf.Cos(leanAngleRad);
            float tanLeanAngle = Mathf.Tan(leanAngleRad);
            float cosCaster = Mathf.Cos(casterAngle * Mathf.Deg2Rad);
            return wheelbase * cosLeanAngle * tanLeanAngle * _gravity / cosCaster * speed * speed;
        }

        /// <summary>
        /// Returns maximum steer angle for given lean angle at current speed.
        /// </summary>
        /// <param name="leanAngle">Lean angle in degrees.</param>
        /// <param name="speed">Speed in m/s.</param>
        /// <param name="wheelbase">Wheelbase in meters.</param>
        /// <param name="lowHighSpeedBlend">Blend between 0 and 1 of low speed and high speed steering.</param>
        /// <returns>Maximum steer angle for given lean angle, both in degrees.</returns>
        private float GetSteerAngleForLeanAngle(float leanAngle, float speed, float wheelbase)
        {
            if (leanAngle < 0.01f && leanAngle > -0.01f)
            {
                return 0.0f;
            }

            float highSpeedTurningRadius = speed / Mathf.Tan(leanAngle * Mathf.Deg2Rad) * _gravity;
            float lowSpeedTurningRadius = wheelbase / Mathf.Sin(leanAngle * Mathf.Deg2Rad);
            float steerAngleResult = GetSteerAngleForTurningRadius(highSpeedTurningRadius, wheelbase);
            return highSpeedTurningRadius;
        }

        /// <summary>
        /// Returns required lean angle for given speed and steer angle.
        /// </summary>
        /// <param name="speed">Speed of the vehicle.</param>
        /// <param name="steerAngle">Handlebar steer angle. Negative for left, positive for right.</param>
        /// <returns>Lean angle in degrees. Negative for left lean and positive for right.</returns>
        private float GetIdealLeanAngle(float speed, float steerAngle)
        {
            _turningRadius = GetTurningRadius(steerAngle, vc.wheelbase);
            float speedSquared = speed * speed;
            return
                Mathf.Abs(Mathf.Atan2(speedSquared, (Mathf.Abs(_turningRadius) * _gravity)) * Mathf.Rad2Deg) *
                -Mathf.Sign(_turningRadius);
        }

        /// <summary>
        /// Returns vehicle turning radius.
        /// </summary>
        /// <param name="steerAngle">Current steer angle.</param>
        /// <param name="wheelbase">Wheelbase of the vehicle.</param>
        /// <returns>Signed turning radius. Negative for left, positive for right turn.</returns>
        private float GetTurningRadius(float steerAngle, float wheelbase)
        {
            // r = wb / Tan(steerAngle) * 2;
            // 
            float tanA = Mathf.Tan(steerAngle * Mathf.Deg2Rad);
            if (tanA < 0.00001f && tanA > -0.00001f)
            {
                return Mathf.Sign(steerAngle) * 1e6f;
            }
            return wheelbase / tanA * 2f;
        }

        /// <summary>
        /// Returns steer angle for given turning radius.
        /// </summary>
        /// <param name="turningRadius">Turning radius in meters.</param>
        /// <param name="wheelbase">Wheelbase in meters.</param>
        /// <returns>Steer angle in degrees.</returns>
        private float GetSteerAngleForTurningRadius(float turningRadius, float wheelbase)
        {
            if (turningRadius < 1e-6f && turningRadius > -1e-6f)
            {
                return 0.0f;
            }
            return Mathf.Atan((2.0f * wheelbase) / turningRadius) * Mathf.Rad2Deg;
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.MotorcycleModule
{
    [CustomPropertyDrawer(typeof(MotorcycleModule))]
    public partial class MotorcycleModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            MotorcycleModule motorcycleModule = SerializedPropertyHelper.GetTargetObjectOfProperty(property) as MotorcycleModule;
            if (motorcycleModule == null)
            {
                drawer.EndProperty();
                return false;
            }

            drawer.BeginSubsection("Steering");
            drawer.Info("Steering settings are set through 'Control > Steering' tab.");
            drawer.EndSubsection();

            drawer.BeginSubsection("Lean");
            drawer.Field("leanAngleSlipCoefficient");
            drawer.Field("leanAngleMaxDelta", true, "deg");
            drawer.Field("maxLeanAngle", true, "deg");
            drawer.Field("maxLeanTorque", true, "N");

            drawer.IncreaseIndent();
            drawer.BeginSubsection("Lean PID Controller");
            drawer.Field("gainProportional");
            drawer.Field("gainIntegral");
            drawer.Field("gainDerivative");
            drawer.Field("leanPIDCoefficient");
            drawer.EndSubsection();
            drawer.DecreaseIndent();

            drawer.EndSubsection();

            drawer.BeginSubsection("Animation");
            drawer.Field("handlebarsTransform");
            drawer.Field("forksTransform");
            drawer.Field("swingarmTransform");
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
