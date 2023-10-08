using System;
using UnityEngine;
using UnityEngine.Events;
using NWH.VehiclePhysics2.Powertrain;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.Trailer
{
    [Serializable]
    public partial class TrailerModule : VehicleModule
    {
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

        public UnityEvent onAttach = new UnityEvent();

        public UnityEvent onDetach = new UnityEvent();

        /// <summary>
        ///     Should the trailer input states be reset when trailer is detached?
        /// </summary>
        [Tooltip("    Should the trailer input states be reset when trailer is detached?")]
        public bool resetInputStatesOnDetach = true;

        /// <summary>
        ///     If enabled the trailer will keep in same gear as the tractor, assuming powertrain on trailer is enabled.
        /// </summary>
        [Tooltip(
            "If enabled the trailer will keep in same gear as the tractor, assuming powertrain on trailer is enabled.")]
        public bool synchronizeGearShifts = false;

        /// <summary>
        ///     Object that will be disabled when trailer is attached and disabled when trailer is detached.
        /// </summary>
        [Tooltip("    Object that will be disabled when trailer is attached and disabled when trailer is detached.")]
        public GameObject trailerStand;

        [NonSerialized]
        private TrailerHitchModule _trailerHitch;


        public TrailerHitchModule TrailerHitch
        {
            get { return _trailerHitch; }
            set { _trailerHitch = value; }
        }


        public override void Initialize()
        {
            vc.input.autoSetInput = false;
            vc.registerWithVehicleChanger = false;

            base.Initialize();
        }


        public override void Start(VehicleController vc)
        {
            base.Start(vc);
            if (onAttach == null)
            {
                onAttach = new UnityEvent();
            }

            if (onDetach == null)
            {
                onDetach = new UnityEvent();
            }
        }


        public override void FixedUpdate()
        {
            if (!Active)
            {
                return;
            }

            if (_trailerHitch == null)
            {
                return;
            }

            if (Active && attached)
            {
                vc.powertrain.transmission.ratio = _trailerHitch.VehicleController.powertrain.transmission.ratio; // Make sure that the ratio is the same for flip input check.
                if (synchronizeGearShifts)
                {
                    Debug.Assert(_trailerHitch.VehicleController.powertrain.transmission.ForwardGearCount == vc.powertrain.transmission.ForwardGearCount &&
                                 _trailerHitch.VehicleController.powertrain.transmission.ReverseGearCount == vc.powertrain.transmission.ReverseGearCount,
                        "When TrailerModule.synchronizeGearShifts is enabled make sure that both truck and trailer have the same number of forward and reverse gears or" +
                        " disable this option.");
                    vc.powertrain.transmission.ShiftInto(_trailerHitch.VehicleController.powertrain.transmission.Gear);
                }

                // Add a little bit of motor torque to unlock the native friction on WheelController (requires 0.1 motor torque)
                if (vc.input.InputSwappedThrottle > 0.05f)
                {
                    foreach (WheelComponent wc in vc.powertrain.wheels)
                    {
                        if (vc.powertrain.Active)
                        {
                            // Add the motor torque incrementally just in case the powertrain on the trailer is enabled,
                            // as not to reset any previously applied torque
                            wc.wheelUAPI.MotorTorque += 0.00015f;
                        }
                        else
                        {
                            wc.wheelUAPI.MotorTorque = 0.00015f;
                        }
                    }
                }
                else
                {
                    foreach (WheelComponent wc in vc.Wheels)
                    {
                        if (!vc.powertrain.Active)
                        {
                            wc.wheelUAPI.MotorTorque = 0f;
                        }
                    }
                }
            }
        }



        public void OnAttach(TrailerHitchModule trailerHitch)
        {
            _trailerHitch = trailerHitch;

            vc.Wake();

            vc.input.autoSetInput = false;

            // Raise trailer stand
            if (trailerStand != null)
            {
                trailerStand.SetActive(false);
            }

            attached = true;

            onAttach.Invoke();
        }


        public void OnDetach()
        {
            if (resetInputStatesOnDetach)
            {
                vc.input.states.Reset();
            }

            vc.input.autoSetInput = false;


            // Lower trailer stand
            if (trailerStand != null)
            {
                trailerStand.SetActive(true);
            }

            // Assume trailer does not have a power source, cut lights.
            vc.effectsManager.lightsManager.Disable();

            _trailerHitch = null;
            vc.Sleep();

            attached = false;

            onDetach.Invoke();
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.Trailer
{
    [CustomPropertyDrawer(typeof(TrailerModule))]
    public partial class TrailerModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.BeginSubsection("Trailer Settings");
            drawer.Field("attachmentPoint");
            drawer.Field("trailerStand");
            drawer.Field("synchronizeGearShifts");
            drawer.EndSubsection();

            drawer.BeginSubsection("Events");
            drawer.Field("onAttach");
            drawer.Field("onDetach");
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}
#endif
