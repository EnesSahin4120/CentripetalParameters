using UnityEngine;
using UnityEngine.Events;

namespace NWH.Common.Vehicles
{
    public abstract class WheelUAPI : MonoBehaviour
    {
        // INPUTS
        public abstract float MotorTorque { get; set; }
        public abstract float BrakeTorque { get; set; }
        public abstract float CounterTorque { get; }
        public abstract float RollingResistanceTorque { get; set; }
        public abstract float SteerAngle { get; set; }


        // PHYSICAL PROPERTIES
        public abstract float Mass { get; set; }
        public abstract float Radius { get; set; }
        public abstract float Width { get; set; }
        public abstract float Inertia { get; set; }
        public abstract float RPM { get; }
        public abstract float AngularVelocity { get; }
        public abstract Vector3 WheelPosition { get; }
        public abstract float Load { get; }
        public abstract float MaxLoad { get; set; }
        public abstract bool IsGrounded { get; }
        public abstract float Damage { get; set; }
        public abstract float Camber { get; }
        public abstract float CamberTop { get; set; }
        public abstract float CamberBottom { get; set; }


        // SPRING
        public abstract float SpringMaxLength { get; set; }
        public abstract float SpringMaxForce { get; set; }
        public abstract float SpringForce { get; }
        public abstract float SpringLength { get; }
        public abstract float SpringCompression { get; }


        // DAMPER
        public abstract float DamperMaxBumpForce { get; set; }
        public abstract float DamperMaxReboundForce { get; set; }
        public abstract float DamperForce { get; }


        // FRICTION
        public abstract FrictionPreset FrictionPreset { get; set; }
        public abstract float LongitudinalFrictionGrip { get; set; }
        public abstract float LongitudinalFrictionStiffness { get; set; }
        public abstract float LateralFrictionGrip { get; set; }
        public abstract float LateralFrictionStiffness { get; set; }


        // LONGITUDINAL FRICTION
        public abstract float LongitudinalSlip { get; }
        public abstract float LongitudinalSpeed { get; }
        public virtual bool IsSkiddingLongitudinally { get => NormalizedLongitudinalSlip > 0.4f; }
        public virtual float NormalizedLongitudinalSlip { get => Mathf.Clamp01(Mathf.Abs(LongitudinalSlip)); }


        // LATERAL FRICTION
        public abstract float LateralSlip { get; }
        public abstract float LateralSpeed { get; }
        public virtual bool IsSkiddingLaterally { get => NormalizedLateralSlip > 0.4f; }
        public virtual float NormalizedLateralSlip { get => Mathf.Clamp01(Mathf.Abs(LateralSlip)); }


        // FRICTION CIRCLE
        public abstract float FrictionCircleShape { get; set; }
        public abstract float FrictionCircleStrength { get; set; }


        // COLLISION
        public abstract Vector3 HitPoint { get; }
        public abstract Vector3 HitNormal { get; }
        public abstract Collider HitCollider { get; }


        // VISUAL
        public abstract GameObject WheelVisual { get; set; }

        public abstract GameObject NonRotatingVisual { get; set; }


        // GENERAL
        public abstract Rigidbody ParentRigidbody { get; }

        public abstract bool AutoSimulate { get; set; }

        public abstract void Step();

        // FUNCTIONS
        public abstract bool IsVisualOnly { get; set; }
    }
}

