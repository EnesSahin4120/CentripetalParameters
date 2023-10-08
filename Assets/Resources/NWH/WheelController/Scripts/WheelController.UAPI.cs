using UnityEngine;
using NWH.Common.Vehicles;


namespace NWH.WheelController3D
{
    public partial class WheelController : WheelUAPI
    {
        [ShowInTelemetry]
        public override float MotorTorque
        {
            get => motorTorque;
            set => motorTorque = value;
        }

        [ShowInTelemetry]
        public override float CounterTorque
        {
            get => counterTorque;
        }

        [ShowInTelemetry]
        public override float BrakeTorque
        {
            get => brakeTorque;
            set => brakeTorque = value;
        }

        [ShowInTelemetry]
        public override float SteerAngle
        {
            get => steerAngle;
            set => steerAngle = value;
        }

        public override float Mass
        {
            get => wheel.mass;
            set
            {
                wheel.mass = Mathf.Clamp(value, 0f, Mathf.Infinity);
                UpdateWheelParams();
            }
        }

        public override float Radius
        {
            get => wheel.radius;
            set
            {
                wheel.radius = value;
                UpdateWheelParams();
            }
        }

        public override float Width
        {
            get => wheel.width;
            set
            {
                wheel.width = value;
                UpdateWheelParams();
            }
        }

        public override float Inertia
        {
            get => wheel.perceivedPowertrainInertia;
            set => wheel.perceivedPowertrainInertia = Mathf.Clamp(value, 0f, Mathf.Infinity);
        }

        [ShowInTelemetry]
        public override float RPM
        {
            get => wheel.rpm;
        }

        public override float AngularVelocity
        {
            get => wheel.angularVelocity;
        }

        public override Vector3 WheelPosition
        {
            get => wheel.worldPosition;
        }

        [ShowInTelemetry]
        public override float Load
        {
            get => load;
        }

        public override float MaxLoad
        {
            get => loadRating;
            set => loadRating = Mathf.Clamp(value, 0f, Mathf.Infinity);
        }

        [ShowInTelemetry]
        public override float Camber
        {
            get => camber;
        }

        public override float CamberTop
        {
            get => camberAtTop;
            set
            {
                camberAtTop = Mathf.Clamp(value, -45f, 45f);
                //if (_initialized) _wheelControllerManager.BlockNativeCollider(1f);
            }
        }

        public override float CamberBottom
        {
            get => camberAtBottom;
            set
            {
                camberAtBottom = Mathf.Clamp(value, -45f, 45f);
                //if (_initialized) _wheelControllerManager.BlockNativeCollider(1f);
            }
        }

        [ShowInTelemetry]
        public override bool IsGrounded
        {
            get => _isGrounded;
        }

        [ShowInTelemetry]
        public override float Damage
        {
            get => _damage;
            set => _damage = Mathf.Clamp01(value);
        }

        public override float SpringMaxLength
        {
            get => spring.maxLength;
            set
            {
                spring.maxLength = Mathf.Clamp(value, 0f, Mathf.Infinity);
            }
        }

        public override float SpringMaxForce
        {
            get => spring.maxForce;
            set
            {
                spring.maxForce = Mathf.Clamp(value, 0f, Mathf.Infinity);
            }
        }

        [ShowInTelemetry]
        public override float SpringForce
        {
            get => spring.force;
        }

        [ShowInTelemetry]
        public override float SpringLength
        {
            get => spring.length;
        }

        public override float SpringCompression
        {
            get => spring.maxLength < 1e-6f ? spring.maxLength = 1e-6f : spring.length / spring.maxLength;
        }

        public override float DamperMaxBumpForce
        {
            get => damper.maxBumpForce;
            set => damper.maxBumpForce = Mathf.Clamp(value, 0f, Mathf.Infinity);
        }

        public override float DamperMaxReboundForce
        {
            get => damper.maxReboundForce;
            set => damper.maxReboundForce = Mathf.Clamp(value, 0f, Mathf.Infinity);
        }

        [ShowInTelemetry]
        public override float DamperForce
        {
            get => damper.force;
        }

        [ShowInTelemetry]
        public override float LongitudinalSlip
        {
            get => forwardFriction.slip;
        }

        [ShowInTelemetry]
        public override float LongitudinalSpeed
        {
            get => forwardFriction.speed;
        }

        [ShowInTelemetry]
        public override float LateralSlip
        {
            get => sideFriction.slip;
        }

        [ShowInTelemetry]
        public override float LateralSpeed
        {
            get => sideFriction.speed;
        }

        public override Vector3 HitPoint
        {
            get
            {
                Vector3 localPoint = _w2lMat.MultiplyPoint3x4(wheelHit.point);
                localPoint.x = 0;
                return _l2wMat.MultiplyPoint3x4(localPoint);
            }
        }

        public override Vector3 HitNormal
        {
            get => wheelHit.normal;
        }

        public override GameObject WheelVisual
        {
            get => wheel.visual;
            set
            {
                wheel.visual = value;
            }
        }

        public override GameObject NonRotatingVisual
        {
            get => wheel.nonRotatingVisual;
            set
            {
                wheel.nonRotatingVisual = value;
                wheel.nonRotatingVisualIsNull = value == null;
            }
        }

        public override Rigidbody ParentRigidbody
        {
            get => parentRigidbody;
        }

        public override Collider HitCollider
        {
            get => wheelHit.collider;
        }

        public override FrictionPreset FrictionPreset
        {
            get => activeFrictionPreset;
            set => activeFrictionPreset = value;
        }

        public override float LongitudinalFrictionGrip
        {
            get => forwardFriction.grip;
            set => forwardFriction.grip = Mathf.Clamp(value, 0f, Mathf.Infinity);
        }

        public override float LongitudinalFrictionStiffness
        {
            get => forwardFriction.stiffness;
            set => forwardFriction.stiffness = Mathf.Clamp(value, 0f, Mathf.Infinity);
        }

        public override float LateralFrictionGrip
        {
            get => sideFriction.grip;
            set => sideFriction.grip = Mathf.Clamp(value, 0f, Mathf.Infinity);
        }

        public override float LateralFrictionStiffness
        {
            get => sideFriction.stiffness;
            set => sideFriction.stiffness = Mathf.Clamp(value, 0f, Mathf.Infinity);
        }

        public override float RollingResistanceTorque
        {
            get => rollingResistanceTorque;
            set => rollingResistanceTorque = Mathf.Clamp(value, 0f, Mathf.Infinity);
        }

        public override float FrictionCircleShape
        {
            get => frictionCircleShape;
            set => frictionCircleShape = Mathf.Clamp(value, 0f, Mathf.Infinity);
        }

        public override float FrictionCircleStrength
        {
            get => frictionCircleStrength;
            set => frictionCircleStrength = Mathf.Clamp(value, 0f, Mathf.Infinity);
        }

        public override bool IsVisualOnly
        {
            get => visualOnlyUpdate;
            set => visualOnlyUpdate = value;
        }

        public override bool AutoSimulate
        {
            get => _autoSimulate;
            set => _autoSimulate = value;
        }
    }
}
