using NWH.Common.Vehicles;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.WheelController3D
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public partial class WheelController : WheelUAPI
    {
        [Tooltip("    Instance of the spring.")]
        [SerializeField]
        public Spring spring = new Spring();

        [Tooltip("    Instance of the damper.")]
        [SerializeField]
        public Damper damper = new Damper();

        [Tooltip("    Instance of the wheel.")]
        [SerializeField]
        public Wheel wheel = new Wheel();

        [Tooltip("    Side (lateral) friction info.")]
        [SerializeField]
        public Friction sideFriction = new Friction();

        [Tooltip("    Forward (longitudinal) friction info.")]
        [SerializeField]
        public Friction forwardFriction = new Friction();

        /// <summary>
        ///     Contains data about the ground contact point. 
        ///     Not valid if !_isGrounded.
        /// </summary>
        [Tooltip("    Contains point in which wheel touches ground. Not valid if !_isGrounded.")]
        [NonSerialized]
        private WheelHit wheelHit = new WheelHit();

        /// <summary>
        ///     Current active friction preset.
        /// </summary>
        [Tooltip("    Current active friction preset.")]
        [SerializeField]
        private FrictionPreset activeFrictionPreset;

        /// <summary>
        ///     Motor torque applied to the wheel in Nm.
        ///     Can be positive or negative.
        /// </summary>
        [Tooltip(
            "Motor torque applied to the wheel. Since NWH Vehicle Physics 2 the value is readonly and setting it will have no effect\r\nsince torque calculation is done inside powertrain solver.")]
        private float motorTorque;

        /// <summary>
        ///     Brake torque applied to the wheel in Nm.
        ///     Must be positive.
        /// </summary>
        [Tooltip("    Brake torque applied to the wheel in Nm.")]
        private float brakeTorque;

        /// <summary>
        ///     The amount of torque returned by the wheel.
        ///     Under no-slip conditions this will be equal to the torque that was input.
        ///     When there is wheel spin, the value will be less than the input torque.
        /// </summary>
        [UnityEngine.Tooltip("    The amount of torque returned by the wheel.\r\n    Under perfect grip conditions this will be equal to the torque that was put down.\r\n    While in air the value will be equal to the source torque minus torque that is result of dW of the wheel.")]
        private float counterTorque;

        /// <summary>
        ///     Current steer angle of the wheel, in deg.
        /// </summary>
        [Tooltip("    Current steer angle of the wheel.")]
        private float steerAngle;

        /// <summary>
        /// Camber angle at the top of the suspension travel, in deg.
        /// </summary>
        [SerializeField]
        [Range(-8f, 8f)]
        private float camberAtTop;

        /// <summary>
        /// Camber angle at the bottom of the suspension travel, in deg.
        /// </summary>
        [SerializeField]
        [Range(-8f, 8f)]
        private float camberAtBottom;

        /// <summary>
        /// Current camber value.
        /// </summary>
        [NonSerialized]
        private float camber;

        /// <summary>
        ///     Tire load in Nm.
        /// </summary>
        [UnityEngine.Tooltip("    Tire load in Nm.")]
        [NonSerialized]
        private float load;

        /// <summary>
        ///     Maximum load the tire is rated for in [N]. 
        ///     Used to calculate friction. Default value is adequate for most cars but 
        ///     larger and heavier vehicles such as semi trucks will use higher values.
        ///     A good rule of the thumb is that this value should be 2x the Load (Debug tab) 
        ///     while vehicle is stationary.
        /// </summary>
        [SerializeField]
        private float loadRating = 5400;

        /// <summary>
        ///     Constant torque acting similar to brake torque.
        ///     Imitates rolling resistance.
        /// </summary>
        [Range(0, 500)]
        [Tooltip("    Constant torque acting similar to brake torque.\r\n    Imitates rolling resistance.")]
        public float rollingResistanceTorque = 30f;

        /// <summary>
        ///     Amount of anti-squat geometry. 
        ///     -1 = There is no anti-squat and full squat torque is applied to the chassis.
        ///     0 = No squat torque is applied to the chassis.
        ///     1 = Anti-squat torque (inverse squat) is applied to the chassis.
        ///     Higher value will result in less vehicle squat/dive under acceleration/braking.
        /// </summary>
        [Range(-1, 1)]
        [UnityEngine.Tooltip("    Amount of anti-squat geometry. \r\n    " +
            "-1 = There is no anti-squat and full squat torque is applied to the chassis.\r\n    " +
            "0 = No squat torque is applied to the chassis.\r\n    " +
            "1 = Anti-squat torque (inverse squat) is applied to the chassis.\r\n    " +
            "Higher value will result in less vehicle squat/dive under acceleration/braking.")]
        public float antiSquat = 0f;

        /// <summary>
        /// Higher the number, higher the effect of longitudinal friction on lateral friction.
        /// If 1, when wheels are locked up or there is wheel spin it will be impossible to steer.
        /// If 0 doughnuts or power slides will be impossible.
        /// The 'accurate' value is 1 but might not be desirable for arcade games.
        /// </summary>
        [UnityEngine.Tooltip("Higher the number, higher the effect of longitudinal friction on lateral friction.\r\n" +
            "If 1, when wheels are locked up or there is wheel spin it will be impossible to steer." +
            "\r\nIf 0 doughnuts or power slides will be impossible.\r\n" +
            "The 'accurate' value is 1 but might not be desirable for arcade games.")]
        [Range(0, 1)]
        [SerializeField]
        private float frictionCircleStrength = 1f;

        /// <summary>
        /// Higer values have more pronounced slip circle effect as the lateral friction will be
        /// decreased with smaller amounts of longitudinal slip (wheel spin).
        /// Realistic is ~1.5-2.
        /// </summary>
        [Range(0.0001f, 3f)]
        [SerializeField]
        [UnityEngine.Tooltip("Higer values have more pronounced slip circle effect as the lateral friction will be\r\ndecreased with smaller amounts of longitudinal slip (wheel spin).\r\nRealistic is ~1.5-2.")]
        private float frictionCircleShape = 1f;

        /// <summary>
        ///     True if wheel touching ground.
        /// </summary>
        [Tooltip("    True if wheel touching ground.")]
        private bool _isGrounded = false;

        /// <summary>
        ///     Root object of the vehicle.
        /// </summary>
        [SerializeField]
        [Tooltip("    Root object of the vehicle.")]
        private GameObject parent;

        /// <summary>
        ///     Rigidbody to which the forces will be applied.
        /// </summary>
        [Tooltip("    Rigidbody to which the forces will be applied.")]
        private Rigidbody parentRigidbody;

        /// <summary>
        /// Is the wheel only being updated for wheel.visual purposes? E.g. multiplayer client.
        /// </summary>
        [UnityEngine.Tooltip("Is the wheel only being updated for wheel.visual purposes? E.g. multiplayer client.")]
        public bool visualOnlyUpdate;

        /// <summary>
        /// Distance as a percentage of the max spring length. Value of 1 means that the friction force will
        /// be applied 1 max spring length above the contact point, and value of 0 means that it will be applied at the
        /// ground level. Value can be >1.
        /// Can be used instead of the anti-roll bar to prevent the vehicle from tipping over in corners
        /// and can be useful in low framerate applications where anti-roll bar might induce jitter.
        /// </summary>
        [UnityEngine.Tooltip("Distance as a percentage of the max spring length. Value of 1 means that the friction force will\r\nbe applied 1 max spring length above the contact point, and value of 0 means that it will be applied at the\r\nground level. Value can be >1.\r\nCan be used instead of the anti-roll bar to prevent the vehicle from tipping over in corners\r\nand can be useful in low framerate applications where anti-roll bar might induce jitter.")]
        public float forceApplicationPointDistance = 0.8f;

        /// <summary>
        /// Disables the motion vectors on the wheel visual to prevent artefacts due to 
        /// the wheel rotation when using PostProcessing.
        /// </summary>
        [UnityEngine.Tooltip("Disables the motion vectors on the wheel visual to prevent artefacts due to \r\nthe wheel rotation when using PostProcessing.")]
        public bool disableMotionVectors = true;

        /// <summary>
        /// The speed coefficient of the spring / suspension extension when not on the ground.
        /// wheel.perceivedPowertrainInertia.e. how fast the wheels extend when in the air.
        /// The setting of 1 will result in suspension fully extending in 1 second, 2 in 0.5s, 3 in 0.333s, etc.
        /// Recommended value is 6-10.
        /// </summary>
        [Range(0.01f, 30f)]
        [UnityEngine.Tooltip("The speed coefficient of the spring / suspension extension when not on the ground.\r\nI.e. how fast the wheels extend when in the air.\r\nThe setting of 1 will result in suspension fully extending in 1 second, 2 in 0.5s, 3 in 0.333s, etc.\r\nRecommended value is 6-10.")]
        public float suspensionExtensionSpeedCoeff = 6f;

        /// <summary>
        /// The amount of wobble around the X-axis the wheel will have when fully damaged.
        /// Part of the damage visualization and does not affect handling.
        /// </summary>
        [Range(0f, 90f)]
        [UnityEngine.Tooltip("The amount of wobble around the X-axis the wheel will have when fully damaged.\r\nPart of the damage visualization and does not affect handling.")]
        public float damageMaxWobbleAngle = 30f;

        /// <summary>
        /// Called when either radius or width of the wheel change.
        /// </summary>
        [NonSerialized]
        public UnityEvent onWheelDimensionsChange = new UnityEvent();

        /// <summary>
        /// Scales the forces applied to other Rigidbodies. Useful for interacting
        /// with lightweight objects and prevents them from flying away or glitching out.
        /// </summary>
        public float otherBodyForceScale = 1f;

        public float loadContribution = 0.25f;


        private bool _autoSimulate = true;
        private Vector3 _hitLocalPoint;
        private Vector3 _hitContactVelocity;
        private Vector3 _hitSurfaceVelocity;
        private Vector3 _hitForwardDirection;
        private Vector3 _hitSidewaysDirection;
        private Rigidbody _hitRigidbody;
        private Vector3 _frictionForce;
        private Vector3 _suspensionForce;
        [HideInInspector] private Matrix4x4 _w2lMat;
        [HideInInspector] private Matrix4x4 _l2wMat;
        private float _damage;
        [HideInInspector] private Transform _transform;
        [HideInInspector] private bool _initialized = false;
        [HideInInspector] private float _dt;
        [HideInInspector] private Vector3 _transformPosition;
        [HideInInspector] private Quaternion _transformRotation;
        [HideInInspector] private Vector3 _transformUp;
        [HideInInspector] private Vector3 _zeroVector;
        [HideInInspector] private Vector3 _upVector;
        [HideInInspector] private Vector3 _forwardVector;
        [HideInInspector] private Vector3 _rightVector;
        [HideInInspector] private Quaternion _localSteerRotation;
        [HideInInspector] private Quaternion _localAxleRotation;
        [HideInInspector] private Quaternion _localDamageRotation;
        [HideInInspector] private Quaternion _localBaseRotation;
        [HideInInspector] private Quaternion _worldBaseRotation;
        [HideInInspector] private Quaternion _camberRotation;
        [HideInInspector] private GroundDetectionBase _groundDetection;
        private List<int> _initColliderLayers;
        private List<GameObject> _vehicleColliderObjects;
        private WheelControllerManager _wheelControllerManager;
        private bool _lowSpeedReferenceIsSet = false;
        private Vector3 _lowSpeedReferencePosition;


        public WheelHit GetWheelHit()
        {
            return wheelHit;
        }


        /// <summary>
        /// Initializes the wheel, suspension and related components.
        /// </summary>
        private void Initialize()
        {
#if UNITY_EDITOR
            if (!SessionState.GetBool("WC3D_ShownDtWarning", false) && Time.fixedDeltaTime > 0.01f)
            {
                Debug.Log($"Time.fixedDeltaTime of {Time.fixedDeltaTime} detected. Recommended value is 0.01667 (60Hz) or less (e.g. 0.01 = 100Hz) for " +
                    $"the best vehicle physics behaviour, especially in high speed games. On mobile games 0.02 (50Hz) is recommended.");
                SessionState.SetBool("WC3D_ShownDtWarning", true);
            }
#endif

            if (parentRigidbody == null)
            {
                parentRigidbody = GetComponentInParent<Rigidbody>();
            }

            Debug.Assert(parentRigidbody != null, $"Parent Rigidbody not found on {name}.");


            _wheelControllerManager = parentRigidbody.GetComponent<WheelControllerManager>();
            if (_wheelControllerManager == null)
            {
                _wheelControllerManager = parentRigidbody.gameObject.AddComponent<WheelControllerManager>();
            }

            if (!_wheelControllerManager.wheelControllers.Contains(this))
            {
                _wheelControllerManager.wheelControllers.Add(this);
            }

            _transform = transform;
            _dt = Time.fixedDeltaTime;

            // Cache vector values
            _zeroVector = Vector3.zero;
            _upVector = Vector3.up;
            _forwardVector = Vector3.forward;
            _rightVector = Vector3.right;

            // Sets the defaults, same as calling Reset()
            SetDefaults();

            // Create an empty wheel visual if not assigned
            if (wheel.visual == null)
            {
                wheel.visual = new GameObject($"{name}_emptyVisual");
                wheel.visualTransform = wheel.visual.transform;
                wheel.visualTransform.parent = _transform;
                wheel.visualTransform.SetPositionAndRotation(_transform.position - _transform.up * (spring.maxLength * 0.5f),
                    _transform.rotation);
            }
            else
            {
                wheel.visualTransform = wheel.visual.transform;
            }

            wheel.nonRotatingVisualIsNull = wheel.nonRotatingVisual == null;

            // v2.0 or newer requires the wheel visual to be parented directly to the WheelController.
            if (wheel.visualTransform.parent != _transform)
            {
                wheel.visualTransform.SetParent(_transform);
            }


            SetupMeshColliders();


            // Disable motion vectors as these can cause issues with PP when wheel is rotating
            if (disableMotionVectors)
            {
                MeshRenderer[] meshRenderers = wheel.visual.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer mr in meshRenderers)
                {
                    mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                }
            }

            // Initialize wheel vectors
            Transform cachedVisualTransform = wheel.visual.transform;
            wheel.worldPosition = cachedVisualTransform.position;
            wheel.localPosition = _transform.InverseTransformPoint(wheel.worldPosition);
            wheel.localPosition.x = 0;
            wheel.localPosition.z = 0;
            wheel.worldPosition = _transform.TransformPoint(wheel.localPosition);
            wheel.up = cachedVisualTransform.up;
            wheel.forward = cachedVisualTransform.forward;
            wheel.right = cachedVisualTransform.right;

            // Initialize matrices here because contact modification runs before FixedUpdate()
            _w2lMat = _transform.worldToLocalMatrix;
            _l2wMat = _transform.localToWorldMatrix;

            // Initialize non-rotating visual
            if (wheel.nonRotatingVisual != null)
            {
                wheel.nonRotatingVisualLocalOffset = _transform.
                    InverseTransformVector(wheel.nonRotatingVisual.transform.position - wheel.visualTransform.position);
            }

            // Initialize spring length to starting value.
            if (spring.maxLength > 0)
            {
                spring.length = -_w2lMat.MultiplyPoint3x4(wheel.visualTransform.position).y;
            }

            // Initialize ground detection
            _groundDetection = GetComponent<GroundDetectionBase>();
            if (_groundDetection == null)
            {
                _groundDetection = gameObject.AddComponent<StandardGroundDetection>();
            }
            wheelHit = new WheelHit();

            // Intialize layers
            _vehicleColliderObjects = new List<GameObject>();
            _initColliderLayers = new List<int>();

            Collider[] colliders = parentRigidbody.gameObject.GetComponentsInChildren<Collider>();
            foreach (Collider c in colliders)
            {
                GameObject go = c.gameObject;
                if (go.layer == 2) continue;
                _vehicleColliderObjects.Add(go);
                _initColliderLayers.Add(go.layer);
            }

            // Remember radius and width for possible runtime changes
            // Not a property so it works through inspector too.
            wheel.prevRadius = wheel.radius;
            wheel.prevWidth = wheel.width;

            // Initialize the wheel params
            UpdateWheelParams();
        }


        /// <summary>
        /// Used to update the wheel parameters (inertia, scale, etc.) after one of the wheel 
        /// dimensions is changed.
        /// </summary>
        private void UpdateWheelParams()
        {
            wheel.inertia = 0.5f * wheel.mass * wheel.radius * wheel.radius;
            wheel.perceivedPowertrainInertia = wheel.inertia;

            // Collider be null when setting up outside of play mode, so do a check.
            if (wheel.bottomMeshCollider != null)
            {
                wheel.bottomMeshCollider.sharedMesh = WheelControllerUtility.CreateWheelMesh(wheel.radius * 0.95f, wheel.width * 0.95f, false);
            }

            if (wheel.topMeshCollider != null)
            {
                wheel.topMeshCollider.sharedMesh = WheelControllerUtility.CreateWheelMesh(wheel.radius, wheel.width, true);
            }
        }


        private void Awake()
        {
            Debug.Assert(transform.localScale == Vector3.one,
                "WheelController scale is not 1. WheelController scale should be [1,1,1].");
        }


        private void Start()
        {
            Initialize();

            onWheelDimensionsChange.AddListener(UpdateWheelParams);

            _initialized = true;
        }


        private void FixedUpdate()
        {
            if (_autoSimulate)
            {
                Step();
            }
        }


        private void OnEnable()
        {
            if (_wheelControllerManager != null && !_wheelControllerManager.wheelControllers.Contains(this))
            {
                _wheelControllerManager.wheelControllers.Add(this);
            }
        }


        private void OnDisable()
        {
            if (_wheelControllerManager != null && _wheelControllerManager.wheelControllers.Contains(this))
            {
                _wheelControllerManager.wheelControllers.Remove(this);
            }
        }


        public override void Step()
        {
            if (!_initialized || !isActiveAndEnabled) return;

            _dt = Time.fixedDeltaTime;

            // Optimization. Ideally visual should be a Transform but backwards compatibility is required.
            wheel.visualTransform = wheel.visual.transform;
            wheel.colliderTransform = wheel.colliderGO.transform;

            // Update matrices
            _w2lMat = _transform.worldToLocalMatrix;
            _l2wMat = _transform.localToWorldMatrix;

            // Update cached values
            _transformPosition = _transform.position;
            _transformRotation = _transform.rotation;
            _transformUp = _l2wMat.MultiplyVector(_upVector);

            // Check if the update is required
            if (_dt < 1e-8f) return;

            // Check for radius change
            if (wheel.prevRadius != wheel.radius || wheel.prevWidth != wheel.width)
            {
                wheel.sizeHasChanged = true; // Used to disable low speed collider after resize
                onWheelDimensionsChange.Invoke();
            }
            wheel.prevRadius = wheel.radius;
            wheel.prevWidth = wheel.width;

            // Find the hit point
            Vector3 origin;
            origin.x = _transformPosition.x + _transformUp.x * wheel.radius * 1.1f;
            origin.y = _transformPosition.y + _transformUp.y * wheel.radius * 1.1f;
            origin.z = _transformPosition.z + _transformUp.z * wheel.radius * 1.1f;

            Vector3 direction;
            direction.x = -_transformUp.x;
            direction.y = -_transformUp.y;
            direction.z = -_transformUp.z;

            float length = wheel.radius * 2.2f + spring.maxLength;

            SetColliderLayersToIgnore();
            _isGrounded = _groundDetection.WheelCast(origin, direction, length, wheel.radius, wheel.width, ref wheelHit);
            ResetColliderLayers();

            _hitLocalPoint = _w2lMat.MultiplyPoint3x4(wheelHit.point);

            // Update the suspension
            PhysicsUpdate();
        }


        public virtual void CalculateFriction()
        {
            forwardFriction.force = 0;
            sideFriction.force = 0;

            float loadSum = 0;
            int wheelControllerCount = _wheelControllerManager.wheelControllers.Count;
            for (int i = 0; i < wheelControllerCount; i++)
            {
                loadSum += _wheelControllerManager.wheelControllers[i].load;
            }
            float loadContribution = loadSum == 0 ? 1f : load / loadSum;

            float invDt = _dt == 0 ? 1f : 1f / _dt;
            float invRadius = wheel.radius == 0 ? 1f : 1f / wheel.radius;
            float invInertia = wheel.perceivedPowertrainInertia == 0 ? 1f : 1f / wheel.perceivedPowertrainInertia;

            float loadClamped = load < 0f ? 0f : load > loadRating ? loadRating : load;
            float forwardLoadFactor = loadClamped * 1.6f;
            float sideLoadFactor = loadClamped * 2f;

            float loadPercent = load / loadRating;
            loadPercent = loadPercent < 0f ? 0f : loadPercent > 1f ? 1f : loadPercent;
            float slipLoadModifier = 1f - loadPercent * 0.4f;

            float mass = parentRigidbody.mass;
            float wheelPlusLinearSpeed = forwardFriction.speed + wheel.angularVelocity * wheel.radius;
            float absWheelPlusLinearSpeed = wheelPlusLinearSpeed < 0 ? -wheelPlusLinearSpeed : wheelPlusLinearSpeed;
            float forwardForceClamp = mass * loadContribution * (absWheelPlusLinearSpeed + 0.01f) * invDt;
            float absSideSpeed = sideFriction.speed < 0 ? -sideFriction.speed : sideFriction.speed;
            float sideForceClamp = mass * loadContribution * absSideSpeed * invDt;
            float absForwardSpeed = forwardFriction.speed < 0f ? -forwardFriction.speed : forwardFriction.speed;
            float clampedAbsForwardSpeed = absForwardSpeed < 5f ? 5f : absForwardSpeed;

            // *******************************
            // ******** LONGITUDINAL ********* 
            // *******************************
            // In this version of the friction friction itself and angular velocity are independent.
            // This results in a somewhat reduced physical accuracy and ignores the tail end of the friction curve
            // but gives better results overall with the most common physics update rates (33Hz - 100Hz) since
            // there is no circular dependency between the angular velocity / slip and force which makes it stable
            // and removes the need for iterative methods. Since the stable state is achieved within one frame it can run 
            // with as low as needed physics update.


            // *** FRICTION ***
            float peakForwardFrictionForce = activeFrictionPreset.BCDE.z * forwardLoadFactor;
            float combinedBrakeTorque = brakeTorque + rollingResistanceTorque;
            combinedBrakeTorque = combinedBrakeTorque < 0 ? 0 : combinedBrakeTorque;
            float forwardFrictionSpeedSign = forwardFriction.speed < 0 ? -1f : 1f;
            float signedCombinedBrakeTorque = combinedBrakeTorque * -forwardFrictionSpeedSign;
            float signedCombinedBrakeForce = signedCombinedBrakeTorque * invRadius;
            float motorForce = motorTorque * invRadius;
            float forwardInputForce = motorForce + signedCombinedBrakeForce;
            float maxForwardForce = peakForwardFrictionForce < forwardForceClamp ? peakForwardFrictionForce : forwardForceClamp;
            forwardFriction.force = forwardInputForce > maxForwardForce ? maxForwardForce
                : forwardInputForce < -maxForwardForce ? -maxForwardForce : forwardInputForce;
            forwardFriction.force *= forwardFriction.grip;


            // *** ANGULAR VELOCITY ***
            // Add input torque to the wheel
            float initAngularVelocity = wheel.angularVelocity;
            float angVelSign = wheel.angularVelocity < 0 ? -1f : 1f;
            float signedWheelBrakeForce = (combinedBrakeTorque / wheel.radius) * -angVelSign;
            float absWheelStopForce = wheel.angularVelocity * wheel.perceivedPowertrainInertia * invRadius * invDt;
            absWheelStopForce = absWheelStopForce < 0 ? -absWheelStopForce : absWheelStopForce;
            absWheelStopForce += maxForwardForce;
            float clampedBrakeForce = signedWheelBrakeForce > absWheelStopForce ? absWheelStopForce : signedWheelBrakeForce < -absWheelStopForce ?
                -absWheelStopForce : signedWheelBrakeForce;
            float wheelInForce = motorForce + clampedBrakeForce;
            wheel.angularVelocity += wheelInForce * wheel.radius * invInertia * _dt;

            // Correct the angular velocity using the available friction budget
            float targetAngularVelocity = forwardFriction.speed * invRadius;
            float spinAngularVelocity = wheel.angularVelocity - targetAngularVelocity;
            float rotationalErrorForce = spinAngularVelocity * wheel.perceivedPowertrainInertia * invRadius * invDt;
            float wheelForce = -rotationalErrorForce;
            wheelForce = wheelForce > maxForwardForce ? maxForwardForce : wheelForce < -maxForwardForce ? -maxForwardForce : wheelForce;

            float absWheelBrakeForce = signedWheelBrakeForce < 0 ? -signedWheelBrakeForce : signedWheelBrakeForce;
            bool isWheelBlocked = _isGrounded && absWheelStopForce < absWheelBrakeForce;

            if (isWheelBlocked)
            {
                wheel.angularVelocity = 0f;
            }
            else
            {
                wheel.angularVelocity += wheelForce * wheel.radius * invInertia * _dt;
            }

            float absAngularVelocity = wheel.angularVelocity < 0 ? -wheel.angularVelocity : wheel.angularVelocity;

            //float velocityDeltaTorque = ((wheel.angularVelocity - initAngularVelocity) * wheel.perceivedPowertrainInertia) * invDt;
            //float velocityDeltaForce = velocityDeltaTorque * wheel.radius;
            //forwardFriction.force -= velocityDeltaForce;

            counterTorque = (-forwardFriction.force + signedCombinedBrakeTorque) * wheel.radius;


            // *** SLIP ***
            // Calculate slip based on the corrected angular velocity
            forwardFriction.slip = (forwardFriction.speed - wheel.angularVelocity * wheel.radius) / clampedAbsForwardSpeed;
            forwardFriction.slip *= forwardFriction.stiffness * slipLoadModifier;



            // *******************************
            // ********** LATERAL ************ 
            // *******************************

            sideFriction.slip = (Mathf.Atan2(sideFriction.speed, clampedAbsForwardSpeed) * Mathf.Rad2Deg) / 90f;
            sideFriction.slip *= sideFriction.stiffness * slipLoadModifier;
            float sideSlipSign = sideFriction.slip < 0 ? -1f : 1f;
            float absSideSlip = sideFriction.slip < 0 ? -sideFriction.slip : sideFriction.slip;
            float peakSideFrictionForce = activeFrictionPreset.BCDE.z * sideLoadFactor;
            float sideForce = -sideSlipSign * activeFrictionPreset.Curve.Evaluate(absSideSlip) * sideLoadFactor;
            sideFriction.force = sideForce;
            sideFriction.force *= sideFriction.grip;



            // *******************************
            // ******* ANTI - CREEP **********
            // *******************************

            // Get the error to the reference point and apply the force to keep the wheel at that point
            if (_isGrounded && absForwardSpeed < 0.1f && absSideSpeed < 0.1f)
            {
                Vector3 currentPosition = _transformPosition - _transformUp * (spring.length + wheel.radius);
                if (!_lowSpeedReferenceIsSet)
                {
                    _lowSpeedReferenceIsSet = true;
                    _lowSpeedReferencePosition = currentPosition;
                }
                else
                {
                    Vector3 referenceError = currentPosition - _lowSpeedReferencePosition;
                    Vector3 correctiveForce = invDt * loadContribution * -mass * referenceError;

                    if (isWheelBlocked && absAngularVelocity < 1f)
                    {
                        forwardFriction.force += Vector3.Dot(correctiveForce, _hitForwardDirection);
                    }

                    sideFriction.force += Vector3.Dot(correctiveForce, _hitSidewaysDirection);
                }
            }
            else
            {
                _lowSpeedReferenceIsSet = false;
            }


            // Clamp the forces once again, this time ignoring the force clamps as the anti-creep forces do not cause jitter,
            // so the forces are limited only by the surface friction.
            forwardFriction.force = forwardFriction.force > peakForwardFrictionForce ? peakForwardFrictionForce
                : forwardFriction.force < -peakForwardFrictionForce ? -peakForwardFrictionForce : forwardFriction.force;

            sideFriction.force = sideFriction.force > peakSideFrictionForce ? peakSideFrictionForce
                : sideFriction.force < -peakSideFrictionForce ? -peakSideFrictionForce : sideFriction.force;


            // *******************************
            // ********* SLIP CIRCLE ********* 
            // *******************************
            if (frictionCircleStrength > 0 && (absForwardSpeed > 2f || absAngularVelocity > 5f))
            {
                float s = forwardFriction.slip / activeFrictionPreset.peakSlip;
                float a = sideFriction.slip / activeFrictionPreset.peakSlip;

                if (Mathf.Sqrt(s * s + a * a) > 1f)
                {
                    float beta = Mathf.Atan2(sideFriction.slip, forwardFriction.slip * frictionCircleShape);
                    float sinBeta = Mathf.Sin(beta);
                    float cosBeta = Mathf.Cos(beta);
                    float absForwardForce = forwardFriction.force < 0 ? -forwardFriction.force : forwardFriction.force;
                    float absSideForce = sideFriction.force < 0 ? -sideFriction.force : sideFriction.force;
                    float f = absForwardForce * cosBeta * cosBeta + absSideForce * sinBeta * sinBeta;

                    float invSlipCircleCoeff = 1f - frictionCircleStrength;
                    forwardFriction.force = invSlipCircleCoeff * forwardFriction.force - frictionCircleStrength * f * cosBeta;
                    sideFriction.force = invSlipCircleCoeff * sideFriction.force - frictionCircleStrength * f * sinBeta;
                }
            }
        }


        /// <summary>
        /// Wheel physics update.
        /// </summary>
        private void PhysicsUpdate()
        {
            // Remember previous frame values
            wheel.prevWorldPosition = wheel.worldPosition;
            wheel.prevAngularVelocity = wheel.angularVelocity;
            spring.prevLength = spring.length;
            spring.prevVelocity = spring.velocity;

            // Make sure that parameters are inside valid ranges to avoid NaN
            if (_dt < 1e-4f) _dt = 1e-4f;
            if (wheel.radius < 1e-4f) wheel.radius = 1e-4f;
            if (wheel.inertia < 1e-4f) wheel.inertia = 1e-4f;
            if (spring.maxLength < 1e-4f) spring.maxLength = 1e-4f;

            // Cache values 
            float invRadius = 1f / wheel.radius;
            float invDt = 1f / _dt;


            // Calculate wheel position and spring length from the hit point
            float localAirYPosition = wheel.localPosition.y - _dt * spring.maxLength * suspensionExtensionSpeedCoeff;
            if (_isGrounded)
            {
                float zDistance = _hitLocalPoint.z;
                float hitAngle = Mathf.Asin(Mathf.Clamp(zDistance * invRadius, -1f, 1f));
                float localGroundedYPosition = _hitLocalPoint.y
                    + wheel.radius * Mathf.Cos(hitAngle);

                wheel.localPosition.y = Mathf.Max(localGroundedYPosition, localAirYPosition);
            }
            else
            {
                wheel.localPosition.y = localAirYPosition;
            }

            // Get spring length
            spring.length = -wheel.localPosition.y;

            // Clamp spring length
            if (spring.length <= 0f)
            {
                spring.extensionState = Spring.ExtensionState.BottomedOut;
                spring.length = 0;
            }
            else if (spring.length >= spring.maxLength)
            {
                spring.extensionState = Spring.ExtensionState.OverExtended;
                spring.length = spring.maxLength;
                _isGrounded = false;
            }
            else
            {
                spring.extensionState = Spring.ExtensionState.Normal;
            }


            // Enable the bottom mesh collider if bottomed out
            wheel.bottomMeshCollider.enabled = spring.extensionState == Spring.ExtensionState.BottomedOut;


            // Calculate spring values
            spring.velocity = (spring.length - spring.prevLength) * invDt;
            spring.compression = (spring.maxLength - spring.length) / spring.maxLength;
            spring.force = _isGrounded ? spring.maxForce * spring.forceCurve.Evaluate(spring.compression) : 0;


            // Calculate damper force
            if (_isGrounded)
            {
                float absSpringVel = spring.velocity < 0 ? -spring.velocity : spring.velocity;
                if (spring.velocity < 0f)
                {
                    damper.force = damper.maxBumpForce * damper.bumpCurve.Evaluate(absSpringVel / 10f) * 10f; // backwards compatibility multiplier
                }
                else
                {
                    damper.force = -damper.maxReboundForce * damper.reboundCurve.Evaluate(absSpringVel / 10f) * 10f;
                }
            }
            else
            {
                damper.force = 0f;
            }


            UpdateWheelValues();


            // Calculate combined suspension force = load
            // When the wheel is bottoming out, calculate this force from the separation inside CME
            if (_isGrounded)
            {
                bool hasSuspension = spring.maxForce > 0 && spring.maxLength > 0;
                load = hasSuspension ? Mathf.Clamp(spring.force + damper.force, 0.0f, Mathf.Infinity) : loadRating;
                _suspensionForce = load * wheelHit.normal;

                if (hasSuspension && !visualOnlyUpdate)
                {
                    parentRigidbody.AddForceAtPosition(_suspensionForce, _transformPosition);
                }
            }
            else
            {
                load = 0;
                _suspensionForce = _zeroVector;
            }


            // Get Rigidbody velocity at the hit point
            if (_isGrounded)
            {
                _hitContactVelocity = parentRigidbody.GetPointVelocity(wheelHit.point);
                _hitForwardDirection = Vector3.Normalize(Vector3.Cross(wheelHit.normal, -wheel.right));
                _hitSidewaysDirection = Quaternion.AngleAxis(90f, wheelHit.normal) * _hitForwardDirection;
                _hitRigidbody = wheelHit.collider.attachedRigidbody;

                if (_hitRigidbody != null)
                {
                    _hitSurfaceVelocity = _hitRigidbody.GetPointVelocity(wheelHit.point);
                    _hitContactVelocity -= _hitSurfaceVelocity;
                }
                else
                {
                    _hitSurfaceVelocity = _zeroVector;
                }
            }


            // Calculate friction
            counterTorque = 0;

            if (visualOnlyUpdate)
            {
                Vector3 wheelPositionDelta;
                wheelPositionDelta.x = wheel.worldPosition.x - wheel.prevWorldPosition.x;
                wheelPositionDelta.y = wheel.worldPosition.y - wheel.prevWorldPosition.y;
                wheelPositionDelta.z = wheel.worldPosition.z - wheel.prevWorldPosition.z;
                Vector3 wheelVelocity = wheelPositionDelta * invDt;
                wheel.angularVelocity = _w2lMat.MultiplyVector(wheelVelocity).z * invRadius;
            }
            else
            {
                // Get forward and side friction speed components
                forwardFriction.speed = _isGrounded ? Vector3.Dot(_hitContactVelocity, _hitForwardDirection) : 0;
                sideFriction.speed = _isGrounded ? Vector3.Dot(_hitContactVelocity, _hitSidewaysDirection) : 0;

                // Update friction
                CalculateFriction();

                // Calculate result force
                if (_isGrounded)
                {
                    _frictionForce.x = _hitSidewaysDirection.x * sideFriction.force + _hitForwardDirection.x * forwardFriction.force;
                    _frictionForce.y = _hitSidewaysDirection.y * sideFriction.force + _hitForwardDirection.y * forwardFriction.force;
                    _frictionForce.z = _hitSidewaysDirection.z * sideFriction.force + _hitForwardDirection.z * forwardFriction.force;

                    // Avoid adding calculated friction when using native friction
                    Vector3 forcePosition;
                    forcePosition.x = wheelHit.point.x + _transformUp.x * forceApplicationPointDistance * spring.maxLength;
                    forcePosition.y = wheelHit.point.y + _transformUp.y * forceApplicationPointDistance * spring.maxLength;
                    forcePosition.z = wheelHit.point.z + _transformUp.z * forceApplicationPointDistance * spring.maxLength;
                    parentRigidbody.AddForceAtPosition(_frictionForce, forcePosition);
                }
                else
                {
                    _frictionForce = _zeroVector;
                }
            }

            // Add squat torque
            if (!visualOnlyUpdate)
            {
                float squatMagnitude = forwardFriction.force * wheel.radius * antiSquat;
                Vector3 squatTorque;
                squatTorque.x = squatMagnitude * wheel.right.x;
                squatTorque.y = squatMagnitude * wheel.right.y;
                squatTorque.z = squatMagnitude * wheel.right.z;

                float chassisTorqueMag = (wheel.prevAngularVelocity - wheel.angularVelocity) * wheel.inertia * invDt;
                Vector3 chassisTorque;
                chassisTorque.x = chassisTorqueMag * wheel.right.x;
                chassisTorque.y = chassisTorqueMag * wheel.right.y;
                chassisTorque.z = chassisTorqueMag * wheel.right.z;

                parentRigidbody.AddTorque(squatTorque + chassisTorque);
            }


            // Apply force to the hit body
            if (!visualOnlyUpdate && _isGrounded)
            {
                if (_hitRigidbody != null)
                {
                    Vector3 totalForce;
                    totalForce.x = -(_frictionForce.x + _suspensionForce.x) * otherBodyForceScale;
                    totalForce.y = -(_frictionForce.y + _suspensionForce.y) * otherBodyForceScale;
                    totalForce.z = -(_frictionForce.z + _suspensionForce.z) * otherBodyForceScale;

                    _hitRigidbody.AddForceAtPosition(totalForce, wheelHit.point);
                }
            }
        }


        /// <summary>
        /// Sets the body colliders to ignore raycast to prevent ray-vehicle interaction.
        /// </summary>
        private void SetColliderLayersToIgnore()
        {
            int colliderObjectCount = _vehicleColliderObjects.Count;
            for (int i = 0; i < colliderObjectCount; i++)
            {
                _vehicleColliderObjects[i].gameObject.layer = 2;
            }
        }

        /// <summary>
        /// Reverts the body colliders to their original layers after SetColliderLayersToIgnore()
        /// </summary>
        private void ResetColliderLayers()
        {
            int colliderObjectCount = _vehicleColliderObjects.Count;
            for (int i = 0; i < colliderObjectCount; i++)
            {
                _vehicleColliderObjects[i].gameObject.layer = _initColliderLayers[i];
            }
        }

        /// <summary>
        /// Updates the wheel positions and rotations.
        /// </summary>
        private void UpdateWheelValues()
        {
            // Get clamped wheel position
            wheel.localPosition.y = -spring.length;
            wheel.worldPosition = _l2wMat.MultiplyPoint3x4(wheel.localPosition);

            float sideSign = -Mathf.Sign(_transform.localPosition.x);

            // Calculate camber
            camber = Mathf.Lerp(camberAtBottom, camberAtTop, spring.compression);
            _camberRotation = Quaternion.Euler(0, 0, camber * sideSign);

            // Update wheel rotations and directions
            wheel.axleAngle = wheel.axleAngle % 360.0f + wheel.angularVelocity * Mathf.Rad2Deg * _dt;

            _localSteerRotation = Quaternion.AngleAxis(steerAngle, _upVector);
            _localAxleRotation = Quaternion.AngleAxis(wheel.axleAngle, _rightVector);
            _localDamageRotation = Quaternion.AngleAxis(_damage * damageMaxWobbleAngle, _upVector);

            // This optimization works under Editor and Mono build. Not needed in IL2CPP.
            //_localBaseRotation = _localDamageRotation * _localSteerRotation * _camberRotation;
            _localBaseRotation.x = _localDamageRotation.w * _localSteerRotation.x + _localDamageRotation.x * _localSteerRotation.w + _localDamageRotation.y * _localSteerRotation.z - _localDamageRotation.z * _localSteerRotation.y;
            _localBaseRotation.y = _localDamageRotation.w * _localSteerRotation.y + _localDamageRotation.y * _localSteerRotation.w + _localDamageRotation.z * _localSteerRotation.x - _localDamageRotation.x * _localSteerRotation.z;
            _localBaseRotation.z = _localDamageRotation.w * _localSteerRotation.z + _localDamageRotation.z * _localSteerRotation.w + _localDamageRotation.x * _localSteerRotation.y - _localDamageRotation.y * _localSteerRotation.x;
            _localBaseRotation.w = _localDamageRotation.w * _localSteerRotation.w - _localDamageRotation.x * _localSteerRotation.x - _localDamageRotation.y * _localSteerRotation.y - _localDamageRotation.z * _localSteerRotation.z;

            _localBaseRotation.x = _localBaseRotation.w * _camberRotation.x + _localBaseRotation.x * _camberRotation.w + _localBaseRotation.y * _camberRotation.z - _localBaseRotation.z * _camberRotation.y;
            _localBaseRotation.y = _localBaseRotation.w * _camberRotation.y + _localBaseRotation.y * _camberRotation.w + _localBaseRotation.z * _camberRotation.x - _localBaseRotation.x * _camberRotation.z;
            _localBaseRotation.z = _localBaseRotation.w * _camberRotation.z + _localBaseRotation.z * _camberRotation.w + _localBaseRotation.x * _camberRotation.y - _localBaseRotation.y * _camberRotation.x;
            _localBaseRotation.w = _localBaseRotation.w * _camberRotation.w - _localBaseRotation.x * _camberRotation.x - _localBaseRotation.y * _camberRotation.y - _localBaseRotation.z * _camberRotation.z;

            //_worldBaseRotation = _transformRotation * _localBaseRotation;
            _worldBaseRotation.x = _transformRotation.w * _localBaseRotation.x + _transformRotation.x * _localBaseRotation.w + _transformRotation.y * _localBaseRotation.z - _transformRotation.z * _localBaseRotation.y;
            _worldBaseRotation.y = _transformRotation.w * _localBaseRotation.y + _transformRotation.y * _localBaseRotation.w + _transformRotation.z * _localBaseRotation.x - _transformRotation.x * _localBaseRotation.z;
            _worldBaseRotation.z = _transformRotation.w * _localBaseRotation.z + _transformRotation.z * _localBaseRotation.w + _transformRotation.x * _localBaseRotation.y - _transformRotation.y * _localBaseRotation.x;
            _worldBaseRotation.w = _transformRotation.w * _localBaseRotation.w - _transformRotation.x * _localBaseRotation.x - _transformRotation.y * _localBaseRotation.y - _transformRotation.z * _localBaseRotation.z;

            //wheel.localRotation = _localBaseRotation * _localAxleRotation;
            wheel.localRotation.x = _localBaseRotation.w * _localAxleRotation.x + _localBaseRotation.x * _localAxleRotation.w + _localBaseRotation.y * _localAxleRotation.z - _localBaseRotation.z * _localAxleRotation.y;
            wheel.localRotation.y = _localBaseRotation.w * _localAxleRotation.y + _localBaseRotation.y * _localAxleRotation.w + _localBaseRotation.z * _localAxleRotation.x - _localBaseRotation.x * _localAxleRotation.z;
            wheel.localRotation.z = _localBaseRotation.w * _localAxleRotation.z + _localBaseRotation.z * _localAxleRotation.w + _localBaseRotation.x * _localAxleRotation.y - _localBaseRotation.y * _localAxleRotation.x;
            wheel.localRotation.w = _localBaseRotation.w * _localAxleRotation.w - _localBaseRotation.x * _localAxleRotation.x - _localBaseRotation.y * _localAxleRotation.y - _localBaseRotation.z * _localAxleRotation.z;

            //wheel.worldRotation = _transformRotation * wheel.localRotation;
            wheel.worldRotation.x = _transformRotation.w * wheel.localRotation.x + _transformRotation.x * wheel.localRotation.w + _transformRotation.y * wheel.localRotation.z - _transformRotation.z * wheel.localRotation.y;
            wheel.worldRotation.y = _transformRotation.w * wheel.localRotation.y + _transformRotation.y * wheel.localRotation.w + _transformRotation.z * wheel.localRotation.x - _transformRotation.x * wheel.localRotation.z;
            wheel.worldRotation.z = _transformRotation.w * wheel.localRotation.z + _transformRotation.z * wheel.localRotation.w + _transformRotation.x * wheel.localRotation.y - _transformRotation.y * wheel.localRotation.x;
            wheel.worldRotation.w = _transformRotation.w * wheel.localRotation.w - _transformRotation.x * wheel.localRotation.x - _transformRotation.y * wheel.localRotation.y - _transformRotation.z * wheel.localRotation.z;


            wheel.up = _worldBaseRotation * _upVector;
            wheel.forward = _worldBaseRotation * _forwardVector;
            wheel.right = _worldBaseRotation * _rightVector;

            // Apply wheel collider position and rotation
            wheel.visualTransform.localPosition = wheel.localPosition;
            wheel.visualTransform.localRotation = wheel.localRotation;

            wheel.colliderTransform.localPosition = wheel.localPosition;
            wheel.colliderTransform.localRotation = _localBaseRotation;

            // Apply rotation and position to the non-rotationg objects if assigned
            if (!wheel.nonRotatingVisualIsNull)
            {
                Vector3 nrvPosition = _l2wMat.MultiplyPoint3x4(wheel.localPosition + _localBaseRotation * wheel.nonRotatingVisualLocalOffset);
                wheel.nonRotatingVisual.transform.SetPositionAndRotation(nrvPosition, _transformRotation * _localBaseRotation);
            }
        }


        private void Reset()
        {
            SetDefaults();

            if (parentRigidbody.mass > 1.1f)
            {
                // Assume 4 as the component count might be wrong at this
                // point and wheels added at a later time.
                int wheelCount = 4;

                float gravity = -Physics.gravity.y;
                float weightPerWheel = parentRigidbody.mass * gravity / wheelCount;

                spring.maxForce = weightPerWheel * 6f;
                damper.maxBumpForce = weightPerWheel * 0.8f;
                damper.maxReboundForce = weightPerWheel * 1f;
                loadRating = weightPerWheel * 2f;
            }
        }


        /// <summary>
        ///     Sets default values if they have not already been set.
        ///     Gets called each time Reset() is called in editor - such as adding the script to a GameObject.
        /// </summary>
        /// <param name="reset">Sets default values even if they have already been set.</param>
        /// <param name="findWheelVisuals">Should script attempt to find wheel visuals automatically by name and position?</param>
        public void SetDefaults(bool reset = false, bool findWheelVisuals = true)
        {
            // Objects
            if (parent == null || reset)
            {
                parent = FindParent();
            }

            Debug.Assert(parent != null,
                         $"Parent Rigidbody of WheelController {name} could not be found. It will have to be assigned manually.");

            // Find parent Rigidbody
            parentRigidbody = parent.GetComponent<Rigidbody>();
            if (parentRigidbody == null)
            {
                parentRigidbody = parent.AddComponent<Rigidbody>();
            }

            Debug.Assert(parentRigidbody != null, "Parent does not contain a Rigidbody.");


            if (wheel == null || reset)
            {
                wheel = new Wheel();
            }

            if (spring == null || reset)
            {
                spring = new Spring();
            }

            if (damper == null || reset)
            {
                damper = new Damper();
            }

            if (forwardFriction == null || reset)
            {
                forwardFriction = new Friction();
            }

            if (sideFriction == null || reset)
            {
                sideFriction = new Friction();
            }

            // Friction preset
            if (activeFrictionPreset == null || reset)
            {
                activeFrictionPreset =
                    Resources.Load<FrictionPreset>("Wheel Controller 3D/Defaults/DefaultFrictionPreset");
            }

            // Curves
            if (spring.forceCurve == null || spring.forceCurve.keys.Length == 0 || reset)
            {
                spring.forceCurve = GenerateDefaultSpringCurve();
            }

            if (damper.bumpCurve == null || damper.bumpCurve.keys.Length == 0 || reset)
            {
                damper.bumpCurve = GenerateDefaultDamperBumpCurve();
            }

            if (damper.reboundCurve == null || damper.reboundCurve.keys.Length == 0 || reset)
            {
                damper.reboundCurve = GenerateDefaultDamperReboundCurve();
            }
        }


        private void SetupMeshColliders()
        {
            // Check for any existing colliders on the visual and remove them
            Collider[] visualColliders = wheel.visual.GetComponentsInChildren<Collider>();
            if (visualColliders.Length > 0)
            {
                for (int i = visualColliders.Length - 1; i >= 0; i--)
                {
                    Destroy(visualColliders[i]);
                }
            }

            // Add wheel mesh collider
            wheel.colliderGO = new GameObject()
            {
                name = transform.name + "_Collider"
            };
            wheel.colliderTransform = wheel.colliderGO.transform;
            wheel.colliderTransform.position = wheel.visualTransform.position;
            wheel.colliderTransform.SetParent(transform);

            wheel.topMeshCollider = wheel.colliderGO.AddComponent<MeshCollider>();
            wheel.topMeshCollider.convex = true;
            wheel.topMeshCollider.gameObject.layer = 2; // Ignore self raycast hit.
            wheel.topMeshCollider.material.bounceCombine = PhysicMaterialCombine.Minimum;
            wheel.topMeshCollider.material.frictionCombine = PhysicMaterialCombine.Minimum;
            wheel.topMeshCollider.material.bounciness = 0;
            wheel.topMeshCollider.material.staticFriction = 0;
            wheel.topMeshCollider.material.dynamicFriction = 0;

            wheel.bottomMeshCollider = wheel.colliderGO.AddComponent<MeshCollider>();
            wheel.bottomMeshCollider.convex = true;
            wheel.bottomMeshCollider.gameObject.layer = 2; // Ignore self raycast hit.
            wheel.bottomMeshCollider.material.bounceCombine = PhysicMaterialCombine.Minimum;
            wheel.bottomMeshCollider.material.frictionCombine = PhysicMaterialCombine.Minimum;
            wheel.bottomMeshCollider.material.bounciness = 0;
            wheel.bottomMeshCollider.material.staticFriction = 0;
            wheel.bottomMeshCollider.material.dynamicFriction = 0;
        }


        private GameObject FindParent()
        {
            return GetComponentInParent<Rigidbody>().gameObject;
        }


        private AnimationCurve GenerateDefaultSpringCurve()
        {
            AnimationCurve ac = new AnimationCurve();
            ac.AddKey(0.0f, 0.0f);
            ac.AddKey(1.0f, 1.0f);
            return ac;
        }


        private AnimationCurve GenerateDefaultDamperBumpCurve()
        {
            AnimationCurve ac = new AnimationCurve();
            ac.AddKey(0f, 0f);
            ac.AddKey(1f, 1f);
            return ac;
        }


        private AnimationCurve GenerateDefaultDamperReboundCurve()
        {
            AnimationCurve ac = new AnimationCurve();
            ac.AddKey(0f, 0f);
            ac.AddKey(1f, 1f);
            return ac;
        }


        /// <summary>
        /// Places the WheelController roughly to the position it should be in, in relation to the wheel visual (if assigned).
        /// </summary>
        public void PositionToVisual()
        {
            if (wheel.visual == null)
            {
                Debug.LogError("Wheel visual not assigned.");
                return;
            }

            Rigidbody rb = GetComponentInParent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("Rigidbody not found in parent.");
                return;
            }

            int wheelCount = GetComponentInParent<Rigidbody>().GetComponentsInChildren<WheelController>().Length;
            if (wheelCount == 0) return;

            // Approximate static load on the wheel.
            float approxStaticLoad = (rb.mass * -Physics.gravity.y) / wheelCount;

            // Approximate the spring travel, not taking spring curve into account.
            float approxSpringTravel = Mathf.Clamp01(approxStaticLoad / spring.maxForce) * spring.maxLength;

            // Position the WheelController transform above the wheel.
            transform.position = wheel.visual.transform.position + rb.transform.up * (spring.maxLength - approxSpringTravel);
        }
    }
}



#if UNITY_EDITOR
namespace NWH.WheelController3D
{
    /// <summary>
    ///     Editor for WheelController.
    /// </summary>
    [CustomEditor(typeof(WheelController))]
    [CanEditMultipleObjects]
    public class WheelControllerEditor : NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI()) return false;

            WheelController wc = target as WheelController;

            float logoHeight = 40f;
            Rect texRect = drawer.positionRect;
            texRect.height = logoHeight;
            drawer.DrawEditorTexture(texRect, "Wheel Controller 3D/Editor/logo_wc3d", ScaleMode.ScaleToFit);
            drawer.Space(logoHeight + 4);


            int tabIndex = drawer.HorizontalToolbar("wc3dMenu",
                                     new[] { "Wheel", "Suspension", "Friction", "Misc", "Debug" }, true, true);

            if (tabIndex == 0) // WHEEL
            {
                drawer.BeginSubsection("Wheel");
                drawer.Field("wheel.radius", true, "m");
                drawer.Field("wheel.width", true, "m");
                drawer.Field("wheel.mass", true, "kg");
                drawer.Field("loadRating", true, "N");
                drawer.Info("It is important to set the load rating correctly as it affects friction drastically.\r\n" +
                    "A value of about 2x of the Load at rest (Debug tab) is a good guidance.");
                drawer.Field("rollingResistanceTorque", true, "Nm");
                drawer.Field("parent");
                drawer.EndSubsection();

                drawer.BeginSubsection("Wheel Model");
                drawer.Field("wheel.visual");
                drawer.Field("wheel.nonRotatingVisual", true, "", "Non-Rotating Visual (opt.)");
                drawer.EndSubsection();
            }
            else if (tabIndex == 1) // SUSPENION
            {
                drawer.BeginSubsection("Spring");
                drawer.Field("spring.maxForce", true, "N@100%");
                if (Application.isPlaying)
                    if (wc != null)
                    {
                        float minRecommended = wc.ParentRigidbody.mass * -Physics.gravity.y / 4f;
                        if (wc.SpringMaxForce < minRecommended)
                            drawer.Info(
                                "MaxForce of Spring is most likely too low for the vehicle mass. Minimum recommended for current configuration is" +
                                $" {minRecommended}N.", MessageType.Warning);
                    }

                if (drawer.Field("spring.maxLength", true, "m").floatValue < Time.fixedDeltaTime * 10f)
                    drawer.Info(
                        $"Minimum recommended spring length for Time.fixedDeltaTime of {Time.fixedDeltaTime} is {Time.fixedDeltaTime * 10f}");

                drawer.Field("spring.forceCurve");
                drawer.Info("X: Spring compression [%], Y: Force coefficient");
                drawer.EndSubsection();

                drawer.BeginSubsection("Damper");
                drawer.Field("damper.maxBumpForce", true, "Ns/m");

                drawer.Field("damper.bumpCurve");
                drawer.Info("X: Spring velocity (normalized) [m/s/10], Y: Force coefficient (normalized)");

                drawer.Field("damper.maxReboundForce", true, "Ns/m");

                drawer.Field("damper.reboundCurve");
                drawer.Info("X: Spring velocity (normalized) [m/s/10], Y: Force coefficient (normalized)");

                drawer.EndSubsection();

                drawer.BeginSubsection("General");
                drawer.Field("suspensionExtensionSpeedCoeff");
                drawer.Field("forceApplicationPointDistance", true, null, "Force App. Point Distance");
                drawer.Field("antiSquat", true, "x100%");
                drawer.Field("camberAtTop", true, "deg");
                drawer.Field("camberAtBottom", true, "deg");
                drawer.EndSubsection();
            }
            else if (tabIndex == 2) // FRICTION
            {
                drawer.BeginSubsection("Friction");
                drawer.Field("activeFrictionPreset");
                drawer.EmbeddedObjectEditor<NUIEditor>(((WheelController)target).FrictionPreset,
                                                       drawer.positionRect);

                drawer.BeginSubsection("Friction Circle");
                drawer.Field("frictionCircleStrength", true, null, "Strength");
                drawer.Field("frictionCircleShape", true, null, "Shape");
                drawer.EndSubsection();

                drawer.BeginSubsection("Longitudinal / Forward");
                drawer.Field("forwardFriction.stiffness", true, "x100 %");
                drawer.Field("forwardFriction.grip", true, "x100 %");
                drawer.EndSubsection();

                drawer.BeginSubsection("Lateral / Sideways");
                drawer.Field("sideFriction.stiffness", true, "x100 %");
                drawer.Field("sideFriction.grip", true, "x100 %");
                drawer.EndSubsection();
                drawer.EndSubsection();
            }
            else if (tabIndex == 3) // MISC
            {
                drawer.BeginSubsection("Actions");
                if (drawer.Button("Position To Visual"))
                {
                    foreach (WheelController target in targets)
                    {
                        target.PositionToVisual();
                    }
                }
                drawer.EndSubsection();

                drawer.BeginSubsection("Multiplayer");
                {
                    drawer.Field("visualOnlyUpdate");
                }
                drawer.EndSubsection();


                drawer.BeginSubsection("Damage");
                {
                    drawer.Field("damageMaxWobbleAngle");
                }
                drawer.EndSubsection();


                drawer.BeginSubsection("Rendering");
                {
                    drawer.Field("disableMotionVectors");
                }
                drawer.EndSubsection();

            }
            else
            {
                drawer.Label($"Is Grounded: {wc.IsGrounded}");
                drawer.Space();

                drawer.Label("Wheel");
                drawer.Label($"\tSteer Angle: {wc.SteerAngle}");
                drawer.Label($"\tMotor Torque: {wc.MotorTorque}");
                drawer.Label($"\tBrake Torque: {wc.BrakeTorque}");
                drawer.Label($"\tAng. Vel: {wc.AngularVelocity}");

                drawer.Label("Friction");
                drawer.Label($"\tLng. Slip: {wc.LongitudinalSlip}");
                drawer.Label($"\tLng. Speed: {wc.forwardFriction.speed}");
                drawer.Label($"\tLng. Force: {wc.forwardFriction.force}");
                drawer.Label($"\tLat. Slip: {wc.LateralSlip}");
                drawer.Label($"\tLat. Speed: {wc.sideFriction.speed}");
                drawer.Label($"\tLat. Force: {wc.sideFriction.force}");

                drawer.Label("Suspension");
                drawer.Label($"\tLoad: {wc.Load}");
                drawer.Label($"\tSpring Length: {wc.SpringLength}");
                drawer.Label($"\tSpring Force: {wc.spring.force}");
                drawer.Label($"\tSpring State: {wc.spring.extensionState}");
                drawer.Label($"\tDamper Force: {wc.damper.force}");
            }

            drawer.EndEditor(this);
            return true;
        }


        public override bool UseDefaultMargins()
        {
            return false;
        }
    }
}

#endif