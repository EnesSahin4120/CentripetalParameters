using UnityEngine;
using NWH.Common.Vehicles;

namespace NWH.Common.Vehicles
{
    [RequireComponent(typeof(WheelCollider))]
    public class WheelColliderUAPI : WheelUAPI
    {
        public GameObject wheelVisual;
        public float width = 0.3f;

        [SerializeField] private WheelCollider _wc;
        [SerializeField] private Rigidbody _rb;

        private UnityEngine.WheelHit _wheelHit;
        private bool _isGrounded;
        private Vector3 _rbVelocity;
        private float _forwardSpeed;
        private float _sideSpeed;
        private float _inertia;
        private float _latFrictionStiffness;
        private float _latFrictionGrip;
        private float _lngFrictionStiffness;
        private float _lngFrictionGrip;

        public override float MotorTorque
        {
            get => _wc.motorTorque;
            set => _wc.motorTorque = value;
        }

        public override float BrakeTorque
        {
            get => _wc.brakeTorque;
            set => _wc.brakeTorque = value;
        }

        public override float SteerAngle
        {
            get => _wc.steerAngle;
            set => _wc.steerAngle = value;
        }

        public override float Mass
        {
            get => _wc.mass;
            set => _wc.mass = value;
        }

        public override float Inertia
        {
            get => _inertia;
            set => Mathf.Clamp(_inertia, 1e-6f, Mathf.Infinity);
        }

        public override float Radius
        {
            get => _wc.radius;
            set => _wc.radius = value;
        }

        public override float Width
        {
            get => width;
            set => width = value;
        }

        public override float RPM
        {
            get => _wc.rpm;
        }

        public override float AngularVelocity
        {
            get => _wc.rpm * 0.10471975512f;
        }

        public override Vector3 WheelPosition
        {
            get => transform.TransformPoint(_wc.center);
        }

        public override float Load
        {
            get => _isGrounded ? _wheelHit.force : 0f;
        }

        public override float MaxLoad
        {
            get => _wc.forwardFriction.extremumValue;
            set
            {
                var forwardFriction = _wc.forwardFriction;
                forwardFriction.extremumValue = value;
                forwardFriction.asymptoteValue = forwardFriction.extremumValue * 0.7f; // TODO - could be solved better
                _wc.forwardFriction = forwardFriction;
            }
        }

        public override float Camber { get => 0f; }

        public override float CamberTop { get => 0f; set { } }

        public override float CamberBottom { get => 0f; set { } }

        public override bool IsGrounded
        {
            get => _isGrounded;
        }

        public override float Damage
        {
            get => 0f;
            set { }
        }

        public override float SpringMaxLength
        {
            get => _wc.suspensionDistance;
            set => _wc.suspensionDistance = value;
        }

        public override float SpringMaxForce
        {
            get => _wc.suspensionSpring.spring;
            set
            {
                JointSpring suspensionSpring = _wc.suspensionSpring;
                suspensionSpring.spring = value;
                _wc.suspensionSpring = suspensionSpring;
            }
        }

        public override float SpringForce
        {
            get => _wc.isGrounded ? _wheelHit.force : 0f;
        }

        public override float SpringLength
        {
            get => -_wc.center.y;
        }

        public override float SpringCompression
        {
            get => SpringLength / SpringMaxLength;
        }

        public override float DamperMaxBumpForce
        {
            get => _wc.suspensionSpring.damper;
            set
            {
                JointSpring suspensionSpring = _wc.suspensionSpring;
                suspensionSpring.damper = value;
                _wc.suspensionSpring = suspensionSpring;
            }
        }

        public override float DamperMaxReboundForce
        {
            get => DamperMaxBumpForce;
            set => DamperMaxBumpForce = value;
        }

        public override float DamperForce
        {
            get => 0f;
        }

        public override float LongitudinalSlip
        {
            get => _isGrounded ? _wheelHit.forwardSlip : 0f;
        }

        public override float LongitudinalSpeed
        {
            get => _forwardSpeed;
        }

        public override float LateralSlip
        {
            get => _isGrounded ? _wheelHit.sidewaysSlip : 0f;
        }

        public override float LateralSpeed
        {
            get => _sideSpeed;
        }

        public override Vector3 HitPoint
        {
            get => _isGrounded ? _wheelHit.point : Vector3.zero;
        }

        public override GameObject WheelVisual
        {
            get => wheelVisual;
            set => wheelVisual = value;
        }

        public override GameObject NonRotatingVisual
        { 
            get => null;
            set { }
        }

        public override Rigidbody ParentRigidbody
        {
            get => _rb;
        }

        public override Vector3 HitNormal
        {
            get
            {
                return _isGrounded ? _wheelHit.normal : Vector3.up;
            }
        }

        public override Collider HitCollider
        {
            get
            {
                return _isGrounded ? _wheelHit.collider : null;
            }
        }

        public override FrictionPreset FrictionPreset
        {
            get => null;
            set { }
        }

        public override float CounterTorque
        {
            get
            {
                float frictionForce = Mathf.Lerp(0f, _wc.forwardFriction.extremumSlip, _wheelHit.forwardSlip / _wc.forwardFriction.extremumSlip) * _wc.forwardFriction.extremumValue;
                float frictionTorque = _wc.radius * frictionForce;
                return -frictionTorque;
            }
        }

        public override float LongitudinalFrictionGrip { get => _lngFrictionGrip; set => _lngFrictionGrip = value; }

        public override float LongitudinalFrictionStiffness { get => _lngFrictionStiffness; set => _lngFrictionStiffness = value; }

        public override float LateralFrictionGrip { get => _latFrictionGrip; set => _latFrictionGrip = value; }

        public override float LateralFrictionStiffness { get => _latFrictionStiffness; set => _latFrictionStiffness = value; }
        public override float RollingResistanceTorque { get; set; }
        public override float FrictionCircleShape { get; set; }
        public override float FrictionCircleStrength { get; set; }
        public override bool IsVisualOnly { get; set; }

        public override bool AutoSimulate { get => true; set { } }

        public override void Step()
        {

        }


        void Initialize()
        {
            _wc = GetComponent<WheelCollider>();
            Debug.Assert(_wc != null, "Can not find WheelCollider. Add WheelCollider to the same object as WheelColliderUAPI.");

            _rb = GetComponentInParent<Rigidbody>();
            Debug.Assert(_rb != null, "Rigidbody not found in parent(s).");

            _wc.mass = 200f;
            _inertia = 0.5f * _wc.mass * _wc.radius * _wc.radius;
        }

        void Reset()
        {
            Initialize();
        }

        void Awake()
        {
            Initialize();
        }

        public void FixedUpdate()
        {
            _isGrounded = _wc.GetGroundHit(out _wheelHit);
            _rbVelocity = _rb.GetPointVelocity(WheelPosition);
            Vector3 localRbVelocity = transform.InverseTransformVector(_rbVelocity);
            _forwardSpeed = localRbVelocity.z;
            _sideSpeed = localRbVelocity.x; // TODO - steering not taken into consideration

            _wc.GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheelVisual.transform.SetPositionAndRotation(pos, rot);
        }
    }

}
