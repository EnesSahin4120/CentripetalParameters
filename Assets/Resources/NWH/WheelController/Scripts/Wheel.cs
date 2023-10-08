using System;
using UnityEngine;

namespace NWH.WheelController3D
{
    /// <summary>
    ///     Contains everything wheel related, including rim and tire.
    /// </summary>
    [Serializable]
    public class Wheel
    {
        /// <summary>
        ///     GameObject representing the visual aspect of the wheel / wheel mesh.
        ///     Should not have any physics colliders attached to it.
        /// </summary>
        [Tooltip(
            "GameObject representing the visual aspect of the wheel / wheel mesh.\r\nShould not have any physics colliders attached to it.")]
        public GameObject visual;

        /// <summary>
        /// Chached value for visual.transform to avoid overhead.
        /// Visual should have been a Transform from the start, but for backwards-compatibility it was left as a GameObject.
        /// </summary>
        public Transform visualTransform;

        /// <summary>
        /// Object containing the wheel MeshColliders.
        /// </summary>
        public GameObject colliderGO;

        /// <summary>
        /// Cached value of colliderGO.transform.
        /// </summary>
        public Transform colliderTransform;

        /// <summary>
        /// Collider covering the top half of the wheel. 
        /// </summary>
        public MeshCollider topMeshCollider;

        /// <summary>
        /// Collider covering the bottom half of the wheel. 
        /// Active only is cases of side collision, bottoming out and native friction.
        /// </summary>
        public MeshCollider bottomMeshCollider;

        /// <summary>
        /// ID of the bottom collider.
        /// </summary>
        public int bottomMeshColliderID;

        /// <summary>
        /// ID of the top collider.
        /// </summary>
        public int topMeshColliderID;

        /// <summary>
        ///     Object representing non-rotating part of the wheel. This could be things such as brake calipers, external fenders,
        ///     etc.
        /// </summary>
        [Tooltip(
            "Object representing non-rotating part of the wheel. This could be things such as brake calipers, external fenders, etc.")]
        public GameObject nonRotatingVisual;

        /// <summary>
        ///     Current angular velocity of the wheel in rad/s.
        /// </summary>
        [Tooltip("    Current angular velocity of the wheel in rad/s.")]
        public float angularVelocity;

        /// <summary>
        ///     Current wheel RPM.
        /// </summary>
        public float rpm
        {
            get { return angularVelocity * 9.55f; }
        }

        /// <summary>
        ///     Forward vector of the wheel in world coordinates.
        /// </summary>
        [Tooltip("    Forward vector of the wheel in world coordinates.")]
        [NonSerialized]
        public Vector3 forward;

        /// <summary>
        ///     Vector in world coordinates pointing to the right of the wheel.
        /// </summary>
        [Tooltip("    Vector in world coordinates pointing to the right of the wheel.")]
        [NonSerialized]
        public Vector3 right;

        /// <summary>
        ///     Wheel's up vector in world coordinates.
        /// </summary>
        [Tooltip("    Wheel's up vector in world coordinates.")]
        [NonSerialized]
        public Vector3 up;

        /// <summary>
        ///     Total inertia of the wheel, including the attached powertrain.
        /// </summary>
        [UnityEngine.Tooltip("    Total inertia of the wheel, including the attached powertrain.")]
        public float perceivedPowertrainInertia;

        public float inertia;

        /// <summary>
        ///     Mass of the wheel. Inertia is calculated from this.
        /// </summary>
        [Tooltip("    Mass of the wheel. Inertia is calculated from this.")]
        public float mass = 20.0f;

        /// <summary>
        ///     Position offset of the non-rotating part.
        /// </summary>
        [Tooltip("    Position offset of the non-rotating part.")]
        public Vector3 nonRotatingVisualLocalOffset;

        /// <summary>
        ///     Total radius of the tire in [m].
        /// </summary>
        [Tooltip("    Total radius of the tire in [m].")]
        [Min(0.001f)]
        public float radius = 0.35f;

        /// <summary>
        ///     Current rotation angle of the wheel visual in regards to it's X axis vector.
        /// </summary>
        [Tooltip("    Current rotation angle of the wheel visual in regards to it's X axis vector.")]
        [NonSerialized]
        public float axleAngle;

        /// <summary>
        ///     Width of the tyre.
        /// </summary>
        [Tooltip("    Width of the tyre.")]
        [Min(0.001f)]
        public float width = 0.25f;

        /// <summary>
        ///     Position of the wheel in world coordinates.
        /// </summary>
        [Tooltip("    Position of the wheel in world coordinates.")]
        [NonSerialized]
        public Vector3 worldPosition; // TODO

        /// <summary>
        ///     Position of the wheel in the previous physics update in world coordinates.
        /// </summary>
        [NonSerialized]
        [UnityEngine.Tooltip("    Position of the wheel in the previous physics update in world coordinates.")]
        public Vector3 prevWorldPosition;

        /// <summary>
        ///     Position of the wheel relative to the WheelController transform.
        /// </summary>
        [NonSerialized]
        [UnityEngine.Tooltip("    Position of the wheel relative to the WheelController transform.")]
        public Vector3 localPosition;

        /// <summary>
        ///     Angular velocity during the previus FixedUpdate().
        /// </summary>
        [NonSerialized]
        [UnityEngine.Tooltip("    Angular velocity during the previus FixedUpdate().")]
        public float prevAngularVelocity;

        /// <summary>
        ///     Rotation of the wheel in world coordinates.
        /// </summary>
        [Tooltip("    Rotation of the wheel in world coordinates.")]
        [NonSerialized]
        public Quaternion worldRotation;

        /// <summary>
        /// Local rotation of the wheel.
        /// </summary>
        [NonSerialized] public Quaternion localRotation;

        /// <summary>
        /// Width of the wheel during the previous frame.
        /// </summary>
        [NonSerialized] public float prevWidth;

        /// <summary>
        /// Radius of the wheel during the previous frame.
        /// </summary>
        [NonSerialized] public float prevRadius;

        /// <summary>
        /// True if radius or width of the wheel have been changed in relation to the previous frame.
        /// </summary>
        [NonSerialized] public bool sizeHasChanged = false;

        /// <summary>
        /// True if non-rotating visual is not assigned.
        /// </summary>
        [NonSerialized] public bool nonRotatingVisualIsNull;
    }
}