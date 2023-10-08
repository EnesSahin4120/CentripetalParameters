using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace NWH.WheelController3D
{
    /// <summary>
    ///     Suspension damper.
    /// </summary>
    [Serializable]
    public class Damper
    {
        /// <summary>
        ///     Bump force of the damper.
        /// </summary>
        [Tooltip("    Bump force of the damper.")]
        [FormerlySerializedAs("bumpForce")]
        public float maxBumpForce = 4000.0f;

        /// <summary>
        ///     Rebound force of the damper.
        /// </summary>
        [FormerlySerializedAs("unitReboundForce")]
        [Tooltip("    Rebound force of the damper.")]
        [FormerlySerializedAs("reboundForce")]
        public float maxReboundForce = 4400.0f;

        /// <summary>
        ///     Curve where X axis represents speed of travel of the suspension and Y axis represents resultant force under bump
        ///     (compression).
        ///     Both values are normalized to [0,1].
        /// </summary>
        [FormerlySerializedAs("curve")]
        [Tooltip(
            "Curve where X axis represents speed of travel of the suspension and Y axis represents resultant force.\r\n" +
            "Both values are normalized to [0,1].")]
        public AnimationCurve bumpCurve;

        /// <summary>
        ///     Curve where X axis represents speed of travel of the suspension and Y axis represents resultant force under
        ///     rebound.
        ///     Both values are normalized to [0,1].
        /// </summary>
        [FormerlySerializedAs("curve")]
        [Tooltip(
            "Curve where X axis represents speed of travel of the suspension and Y axis represents resultant force.\r\n" +
            "Both values are normalized to [0,1].")]
        public AnimationCurve reboundCurve;

        /// <summary>
        ///     Current damper force.
        /// </summary>
        [Tooltip("    Current damper force.")]
        public float force;


    }
}