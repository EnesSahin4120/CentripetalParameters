using System;
using System.Collections;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.FlipOver
{
    /// <summary>
    ///     Flip over module. Flips the vehicle over to be the right side up if needed.
    /// </summary>
    [Serializable]
    public partial class FlipOverModule : VehicleModule
    {
        public enum FlipOverType { Gradual, Instant }

        public enum FlipOverActivation { Manual, Automatic }

        /// <summary>
        /// Determines how the vehicle will be flipped over. 
        /// </summary>
        [UnityEngine.Tooltip("Determines how the vehicle will be flipped over. ")]
        public FlipOverType flipOverType = FlipOverType.Instant;

        public FlipOverActivation flipOverActivation = FlipOverActivation.Manual;

        /// <summary>
        ///     Minimum angle that the vehicle needs to be at for it to be detected as flipped over.
        /// </summary>
        [Tooltip("    Minimum angle that the vehicle needs to be at for it to be detected as flipped over.")]
        public float allowedAngle = 70f;

        /// <summary>
        /// If using instant (not gradual) flip over this value will be applied to the transform.y position to prevent rotating
        /// the object to a position that is underground.
        /// </summary>
        [UnityEngine.Tooltip("If using instant (not gradual) flip over this value will be applied to the transform.y position to prevent rotating\r\nthe object to a position that is underground.")]
        public float instantFlipOverVerticalOffset = 1f;

        /// <summary>
        ///     Is the vehicle flipped over?
        /// </summary>
        [Tooltip("    Is the vehicle flipped over?")]
        public bool flippedOver;

        /// <summary>
        ///     Flip over detection will be disabled if velocity is above this value [m/s].
        /// </summary>
        [Tooltip("    Flip over detection will be disabled if velocity is above this value [m/s].")]
        public float maxDetectionSpeed = 0.6f;

        /// <summary>
        ///     Time after detecting flip over after which vehicle will be flipped back.
        /// </summary>
        [Tooltip(
            "Time after detecting flip over after which vehicle will be flipped back or the manual button can be used.")]
        public float timeout = 1f;

        public float flipOverDuration = 5f;

        private bool _flipOverInProgress = false;

        public override void Initialize()
        {
            base.Initialize();

            vc.StartCoroutine(FlipOverCheckCoroutine());
        }


        private IEnumerator FlipOverCheckCoroutine()
        {
            while (true)
            {
                float vehicleAngle = Vector3.Angle(vc.transform.up, -Physics.gravity.normalized);
                flippedOver = vc.Speed < maxDetectionSpeed
                             && vc.vehicleRigidbody.angularVelocity.magnitude < maxDetectionSpeed
                             && vehicleAngle > allowedAngle;

                if (!_flipOverInProgress && flippedOver
                    && ((vc.input.FlipOver && flipOverActivation == FlipOverActivation.Manual) || flipOverActivation == FlipOverActivation.Automatic))
                {
                    if (flipOverType == FlipOverType.Gradual)
                    {
                        vc.StartCoroutine(FlipOverGraduallyCoroutine());
                    }
                    else
                    {
                        FlipOverInstantly();
                    }
                }

                vc.input.FlipOver = false;

                yield return new WaitForSeconds(timeout);
            }
        }


        private IEnumerator FlipOverGraduallyCoroutine()
        {
            float timer = 0;
            RigidbodyConstraints initConstraints = vc.vehicleRigidbody.constraints;
            Quaternion initRotation = vc.transform.rotation;
            Quaternion targetRotation = Mathf.Abs(Vector3.Dot(vc.transform.forward, Vector3.up)) < 0.7f ?
                Quaternion.LookRotation(vc.transform.forward, Vector3.up) :
                Quaternion.LookRotation(vc.transform.up, Vector3.up);

            while (timer < 20f)
            {
                float progress = timer / flipOverDuration;
                if (progress > 1f)
                {
                    vc.vehicleRigidbody.constraints = initConstraints;
                    _flipOverInProgress = false;
                    break;
                }

                vc.vehicleRigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                vc.transform.rotation = Quaternion.Slerp(initRotation, targetRotation, progress);
                _flipOverInProgress = true;

                timer += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            yield return null;
        }


        private void FlipOverInstantly()
        {
            Quaternion targetRotation = Mathf.Abs(Vector3.Dot(vc.transform.forward, Vector3.up)) < 0.7f ?
                Quaternion.LookRotation(vc.transform.forward, Vector3.up) :
                Quaternion.LookRotation(vc.transform.up, Vector3.up);
            vc.transform.rotation = targetRotation;
            vc.transform.position += Vector3.up * instantFlipOverVerticalOffset;
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.FlipOver
{
    [CustomPropertyDrawer(typeof(FlipOverModule))]
    public partial class FlipOverModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("flipOverActivation");
            drawer.Field("flipOverType");
            drawer.Field("instantFlipOverVerticalOffset");
            drawer.Field("timeout");
            drawer.Field("allowedAngle");
            drawer.Field("maxDetectionSpeed");
            drawer.Field("flippedOver", false);

            drawer.EndProperty();
            return true;
        }
    }
}
#endif
