using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.Modules.Trailer
{
    /// <summary>
    ///     Module representing the towing vehicle.
    ///     When a trailer is instantiated after initialization, running SyncTrailers() manually is required for the script to
    ///     find trailers.
    /// </summary>
    [Serializable]
    public partial class TrailerHitchModule : VehicleModule
    {
        /// <summary>
        ///     Maximum distance between towing vehicle's attachment point and trailer's attachment point.
        /// </summary>
        [Tooltip("    Maximum distance between towing vehicle's attachment point and trailer's attachment point.")]
        public float attachDistanceThreshold = 0.5f;

        /// <summary>
        ///     True if object is trailer and is attached to a towing vehicle and also true if towing vehicle and has trailer
        ///     attached.
        /// </summary>
        [Tooltip(
            "True if object is trailer and is attached to a towing vehicle and also true if towing vehicle and has trailer\r\nattached.")]
        public bool attached;

        /// <summary>
        ///     If the vehicle is a trailer, this is the object placed at the point at which it will connect to the towing vehicle.
        ///     If the vehicle is towing, this is the object placed at point at which trailer will be coneected.
        /// </summary>
        [Tooltip(
            "If the vehicle is a trailer, this is the object placed at the point at which it will connect to the towing vehicle." +
            " If the vehicle is towing, this is the object placed at point at which trailer will be coneected.")]
        public Transform attachmentPoint;

        /// <summary>
        ///     If a trailer is in range when the scene is started it will be attached.
        /// </summary>
        [Tooltip("    If a trailer is in range when the scene is started it will be attached.")]
        public bool attachOnPlay;

        /// <summary>
        ///     Breaking force of the generated joint.
        /// </summary>
        [Tooltip("    Breaking force of the generated joint.")]
        public float breakForce = Mathf.Infinity;

        /// <summary>
        ///     Can the trailer be detached once it is attached?
        /// </summary>
        [Tooltip("    Can the trailer be detached once it is attached?")]
        public bool detachable = true;

        /// <summary>
        ///     Power reduction that will be applied when vehicle has no trailer to avoid wheel spin when controlled with a binary
        ///     controller.
        /// </summary>
        [Tooltip(
            "Power reduction that will be applied when vehicle has no trailer to avoid wheel spin when controlled with a binary controller.")]
        public float noTrailerPowerCoefficient = 1f;

        public UnityEvent onTrailerAttach = new UnityEvent();
        public UnityEvent onTrailerDetach = new UnityEvent();

        /// <summary>
        ///     Is trailer's attachment point close enough to be attached to the towing vehicle?
        /// </summary>
        [Tooltip("    Is trailer's attachment point close enough to be attached to the towing vehicle?")]
        public bool trailerInRange;

        /// <summary>
        ///     Use for articulated busses and equipment where rotation around vertical axis is not wanted.
        /// </summary>
        [Tooltip("    Use for articulated busses and equipment where rotation around vertical axis is not wanted.")]
        public bool useHingeJoint;

        [NonSerialized]
        private ConfigurableJoint _configurableJoint;

        [NonSerialized]
        private TrailerModule _nearestTrailerModule;

        /// <summary>
        ///     A trailer that is attached to this trailer hitch
        /// </summary>
        [NonSerialized] private TrailerModule _trailer;

        [NonSerialized]
        private List<TrailerModule> _trailerModules = new List<TrailerModule>();


        public TrailerModule Trailer
        {
            get { return _trailer; }
            set { _trailer = value; }
        }


        public override void Initialize()
        {
            FindSceneTrailerModules(ref _trailerModules);

            if (attachOnPlay)
            {
                vc.StartCoroutine(AttachTrailerWithDelay());
            }

            base.Initialize();
        }


        private IEnumerator AttachTrailerWithDelay()
        {
            yield return new WaitForSeconds(0.2f);

            AttachTrailer(vc);

            yield return null;
        }


        public override void Start(VehicleController vc)
        {
            base.Start(vc);

            if (onTrailerAttach == null)
            {
                onTrailerAttach = new UnityEvent();
            }

            if (onTrailerDetach == null)
            {
                onTrailerDetach = new UnityEvent();
            }

            _trailer = null;
            _nearestTrailerModule = null;
            attached = false;
        }


        public override void Update()
        {
            if (_trailer != null && _trailer.VehicleController != null)
            {
                _trailer.VehicleController.input.states = vc.input.states;

                _trailer.VehicleController.effectsManager.lightsManager.SetStateFromInt(
                    vc.effectsManager.lightsManager.GetIntState()
                );
            }
        }


        public override void FixedUpdate()
        {
            if (!Active)
            {
                return;
            }

            // Find nearby trailers
            if (!attached)
            {
                FindNearestTrailerModule();
            }

            // Attach trailer
            if (!attached && vc.input.TrailerAttachDetach)
            {
                AttachTrailer(vc);
            }
            // Detach trailer
            else if (attached && vc.input.TrailerAttachDetach)
            {
                DetachTrailer(vc);
            }

            // Detach trailer if joint broke
            if (attached && _configurableJoint == null)
            {
                DetachTrailer(vc);
            }

            vc.input.TrailerAttachDetach = false;
        }


        public override void Enable()
        {
            base.Enable();

            if (vc != null)
            {
                vc.powertrain.engine.powerModifiers.Add(NoTrailerPowerModifier);
            }
        }


        public override void Disable()
        {
            base.Disable();

            if (vc != null)
            {
                vc.powertrain.engine.powerModifiers.Remove(NoTrailerPowerModifier);
            }
        }



        public float NoTrailerPowerModifier()
        {
            if (attached)
            {
                return 1f;
            }

            return noTrailerPowerCoefficient;
        }


        /// <summary>
        ///     Updates the list of trailers in the scene.
        ///     Should be called each time a new trailer is instantiated.
        /// </summary>
        public void SyncTrailers()
        {
            FindSceneTrailerModules(ref _trailerModules);
        }


        /// <summary>
        /// Finds the nearest TrailerModule to this vehicle.
        /// Call SyncTrailers() if the trailer has been added after initialization of this module.
        /// </summary>
        /// <returns></returns>
        private TrailerModule FindNearestTrailerModule()
        {
            trailerInRange = false;
            if (attachmentPoint != null) // Check if attachment point exists
            {
                if (_trailerModules.Count > 0)
                {
                    foreach (TrailerModule trailerModule in _trailerModules)
                    {
                        Vector3 dir = trailerModule.attachmentPoint.transform.position -
                                      attachmentPoint.transform.position;
                        float dist = dir.sqrMagnitude;

                        if (dist < attachDistanceThreshold)
                        {
                            trailerInRange = true;
                            return trailerModule;
                        }
                    }
                }
            }

            return null;
        }


        public void AttachTrailer(VehicleController vc)
        {
            SyncTrailers();

            if (_trailerModules.Count == 0)
            {
                Debug.LogWarning("Trying to attach a trailer. No trailers (TrailerModules) found.");
                return;
            }

            _nearestTrailerModule = FindNearestTrailerModule();

            if (_nearestTrailerModule != null)
            {
                _trailer = _nearestTrailerModule;
                if (_trailer == null)
                {
                    Debug.LogWarning("Trying to attach null trailer.");
                    return;
                }

                VehicleController trailerVc = _trailer.VehicleController;

                // Position trailer
                trailerVc.vehicleTransform.position = trailerVc.transform.position -
                                                      (_trailer.attachmentPoint.transform.position -
                                                       attachmentPoint.transform.position);

                // Configure joint
                _configurableJoint = trailerVc.gameObject.GetComponent<ConfigurableJoint>();
                if (_configurableJoint == null)
                {
                    _configurableJoint = trailerVc.gameObject.AddComponent<ConfigurableJoint>();
                }

                _configurableJoint.connectedBody = vc.vehicleRigidbody;
                _configurableJoint.anchor =
                    trailerVc.transform.InverseTransformPoint(attachmentPoint.transform.position);
                _configurableJoint.xMotion = ConfigurableJointMotion.Locked;
                _configurableJoint.yMotion = ConfigurableJointMotion.Locked;
                _configurableJoint.zMotion = ConfigurableJointMotion.Locked;
                _configurableJoint.angularZMotion =
                    useHingeJoint ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
                _configurableJoint.enableCollision = true;
                _configurableJoint.breakForce = breakForce;

                // Reset input flag
                vc.input.TrailerAttachDetach = false;

                // Enable lights if tractor has them enabled
                if (vc.effectsManager.lightsManager.IsEnabled)
                {
                    trailerVc.effectsManager.lightsManager.Enable();
                }
                else
                {
                    trailerVc.effectsManager.lightsManager.Disable();
                }

                attached = true;

                // Disable freeze and constrain on towing vehicle to prevent issues with joints and constrained vehicles.
                //_wasConstrainIdleEnabled   = vc.freezeWhileIdle;
                //vc.freezeWhileIdle       = false;

                _trailer.OnAttach(this);

                onTrailerAttach.Invoke();
            }
        }


        public void DetachTrailer(VehicleController vc)
        {
            if (!detachable || _trailer == null || _trailer.VehicleController == null)
            {
                return;
            }

            attached = false;

            if (_configurableJoint != null)
            {
                Object.Destroy(_configurableJoint);
                _configurableJoint = null;
            }

            _trailer.OnDetach();
            _trailer = null;

            //vc.freezeWhileIdle   = _wasConstrainIdleEnabled;

            vc.input.TrailerAttachDetach = false;

            onTrailerDetach.Invoke();
        }


        /// <summary>
        /// Finds the trailer modules in the scene.
        /// </summary>
        private void FindSceneTrailerModules(ref List<TrailerModule> trailerModules)
        {
            if (trailerModules == null) trailerModules = new List<TrailerModule>();
            else trailerModules.Clear();

            foreach (TrailerModuleWrapper trailerModuleWrapper in Object.FindObjectsOfType<TrailerModuleWrapper>())
            {
                TrailerModule trailerModule = trailerModuleWrapper.module;
                if (trailerModule != null)
                {
                    trailerModules.Add(trailerModule);
                }
            }
        }
    }
}

#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Modules.Trailer
{
    [CustomPropertyDrawer(typeof(TrailerHitchModule))]
    public partial class TrailerHitchModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.BeginSubsection("Attachment");
            drawer.Field("attachmentPoint");
            drawer.Field("attachDistanceThreshold");
            drawer.Field("attachOnPlay");
            drawer.Field("detachable");
            drawer.Field("trailerInRange", false);
            drawer.EndSubsection();

            drawer.BeginSubsection("Joint");
            drawer.Field("breakForce");
            drawer.Field("useHingeJoint");
            drawer.EndSubsection();

            drawer.BeginSubsection("Powertrain");
            drawer.Field("noTrailerPowerCoefficient");
            drawer.EndSubsection();

            drawer.BeginSubsection("Events");
            drawer.Field("onTrailerAttach");
            drawer.Field("onTrailerDetach");
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
