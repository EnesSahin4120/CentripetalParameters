using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.Aerodynamics
{
    /// <summary>
    ///     Aerodynamics module.
    ///     Calculates and applies drag and downforce to the vehicle.
    /// </summary>
    [Serializable]
    public partial class AerodynamicsModule : VehicleModule
    {
        /// <summary>
        ///     Density of air.
        /// </summary>
        public const float RHO = 1.225f;

        public Vector3 dimensions = new Vector3(2f, 1.5f, 4.5f);

        /// <summary>
        ///     The amount of drag that will be added when the vehicle is fully damaged.
        ///     0.5 equals +50% on top of the original, undamaged, drag value.
        /// </summary>
        [Range(0, 5)]
        [Tooltip(
            "The amount of drag that will be added when the vehicle is fully damaged.\r\n0.5 equals +50% on top of the original, undamaged, drag value.")]
        public float damageDragEffect = 0.5f;

        /// <summary>
        ///     Points at which downforce will be applied.
        ///     Avoid applying force at too high positions as that will negatively influence suspension and steering.
        /// </summary>
        [Tooltip(
            "Points at which downforce will be applied.\r\nAvoid applying force at too high positions as that will negatively influence suspension and steering.")]
        public List<DownforcePoint> downforcePoints = new List<DownforcePoint>();

        /// <summary>
        ///     Coefficient of drag of the vehicle's frontal profile.
        ///     Also used for reverse.
        /// </summary>
        [Range(0f, 1f)]
        [Tooltip("    Coefficient of drag of the vehicle's frontal profile.\r\n    Also used for reverse.")]
        public float frontalCd = 0.35f;

        /// <summary>
        ///     Speed in [m/s] at which the downforce will reach it's maximum value
        ///     assigned under downforce points settings.
        /// </summary>
        [Tooltip(
            "Speed in [m/s] at which the downforce will reach it's maximum value\r\nassigned under downforce points settings.")]
        public float maxDownforceSpeed = 80f;

        /// <summary>
        ///     Coefficient of drag of the vehicle's side profile.
        /// </summary>
        [Range(0f, 2f)]
        [Tooltip("    Coefficient of drag of the vehicle's side profile.")]
        public float sideCd = 1.05f;

        /// <summary>
        ///     Should downforce be calculated?
        /// </summary>
        [Tooltip("    Should downforce be calculated?")]
        public bool simulateDownforce;

        /// <summary>
        ///     Should drag be calculated?
        /// </summary>
        [Tooltip("    Should drag be calculated?")]
        public bool simulateDrag = true;

        private float _forwardSpeed;
        private float _frontalArea;

        private float _sideArea;
        private float _sideSpeed;

        /// <summary>
        ///     Drag force in the lateral (side) direction.
        /// </summary>
        [SerializeField]
        private float lateralDragForce;

        /// <summary>
        ///     Drag force in the longitudinal (forward) direction.
        /// </summary>
        [SerializeField]
        private float longitudinalDragForce;


        public override void FixedUpdate()
        {
            if (!Active)
            {
                return;
            }

            if (vc.Speed < 1f)
            {
                longitudinalDragForce = 0;
                lateralDragForce = 0;
                return;
            }

            // Drag
            if (simulateDrag)
            {
                _frontalArea = dimensions.x * dimensions.y * 0.85f;
                _sideArea = dimensions.y * dimensions.z * 0.8f;
                _forwardSpeed = vc.LocalVelocity.z;
                _sideSpeed = vc.LocalVelocity.x;
                float damageDragCoeff =
                    vc.damageHandler.IsEnabled ? 1f + vc.damageHandler.Damage * damageDragEffect : 1;
                longitudinalDragForce = 0.5f * RHO * _frontalArea * frontalCd * (_forwardSpeed * _forwardSpeed) *
                                        (_forwardSpeed > 0 ? -1f : 1f) * damageDragCoeff;
                lateralDragForce = 0.5f * RHO * _sideArea * sideCd * (_sideSpeed * _sideSpeed) *
                                   (_sideSpeed > 0 ? -1f : 1f);
                vc.vehicleRigidbody.AddRelativeForce(new Vector3(lateralDragForce, 0, longitudinalDragForce));
            }

            // Downforce
            if (simulateDownforce)
            {
                float speedPercent = vc.Speed / maxDownforceSpeed;
                float forceCoeff = 1f - (1f - Mathf.Pow(speedPercent, 2f));

                foreach (DownforcePoint dp in downforcePoints)
                {
                    vc.vehicleRigidbody.AddForceAtPosition(forceCoeff * dp.maxForce * -vc.transform.up,
                                                            vc.transform.TransformPoint(dp.position));
                }
            }
        }


        public override void OnDrawGizmosSelected(VehicleController vc)
        {
            foreach (DownforcePoint dp in downforcePoints)
            {
                Gizmos.color = Color.red;
                Vector3 pos = vc.transform.TransformPoint(dp.position);
                Gizmos.DrawSphere(vc.transform.TransformPoint(dp.position), 0.1f);
#if UNITY_EDITOR
                Handles.Label(pos, new GUIContent("Downforce Point"));
#endif
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.Aerodynamics
{
    [CustomPropertyDrawer(typeof(AerodynamicsModule))]
    public partial class AerodynamicsModuleDrawer : ComponentNUIPropertyDrawer
    {
        private float infoHeight;


        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.BeginSubsection("Vehicle Dimensions");
            drawer.Field("dimensions", true, "m");
            drawer.EndSubsection();


            drawer.BeginSubsection("Drag");
            drawer.Field("simulateDrag");
            drawer.Field("frontalCd");
            drawer.Field("sideCd");
            drawer.Field("longitudinalDragForce", false, "N");
            drawer.Field("lateralDragForce", false, "N");
            drawer.Info("To change vehicle dimensions go to 'Other > Settings' tab.");
            drawer.EndSubsection();

            drawer.BeginSubsection("Downforce");
            drawer.Field("simulateDownforce");
            drawer.Field("maxDownforceSpeed", true, "m/s");
            drawer.ReorderableList("downforcePoints");
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
