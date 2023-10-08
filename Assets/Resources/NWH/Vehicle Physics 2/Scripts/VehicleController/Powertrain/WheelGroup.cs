using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using NWH.Common.Vehicles;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Powertrain.Wheel
{
    [Serializable]
    public partial class WheelGroup
    {
        /// <summary>
        ///     Should Ackerman steering angle be added to the axle?
        ///     angle is auto-calculated.
        /// </summary>
        [UnityEngine.Tooltip("    Should Ackerman steering angle be added to the axle?\r\n    angle is auto-calculated.")]
        [ShowInSettings("Add Ackerman")]
        public bool addAckerman = true;


        /// <summary>
        ///     If set to 1 group will receive full brake torque as set by Max Torque parameter under Brake section while 0
        ///     means no breaking at all.
        /// </summary>
        [Tooltip(
            "If set to 1 axle will receive full brake torque as set by Max Torque parameter under Brake section while " +
            "0 means no breaking at all.")]
        [Range(0f, 1f)]
        [ShowInSettings("Brake Coeff.", 0f, 1f, 0.1f)]
        public float brakeCoefficient = 1f;

        /// <summary>
        ///     If set to 1 axle will receive full brake torque when handbrake is used.
        /// </summary>
        [Range(0f, 2f)]
        [Tooltip("    If set to 1 axle will receive full brake torque when handbrake is used.")]
        [ShowInSettings("Brake Coeff.", 0f, 1f, 0.1f)]
        public float handbrakeCoefficient;

        [Tooltip(
            "Setting to true will override camber settings and camber will be calculated from position of the (imaginary) axle object instead.")]
        [ShowInSettings]
        public bool isSolid;

        public string name;

        /// <summary>
        ///     Track width of the axle. 0 if wheel count is not 2.
        /// </summary>
        [Tooltip("    Track width of the axle. 0 if wheel count is not 2.")]
        public float trackWidth;

        [Tooltip(
            "Determines what percentage of the steer angle will be applied to the wheel. If set to negative value" +
            " wheels will turn in direction opposite of input.")]
        [Range(-1f, 1f)]
        [ShowInSettings("Steer Coeff.", -1f, 1f, 0.1f)]
        public float steerCoefficient;

        [Tooltip(
            "Positive caster means that whe wheel will be angled towards the front of the vehicle while negative " +
            " caster will angle the wheel in opposite direction (shopping cart wheel).")]
        [Range(-8f, 8f)]
        [ShowInTelemetry()]
        [SerializeField]
        private float _casterAngle;

        [Tooltip(
            "Positive toe angle means that the wheels will face inwards (front of the wheel angled toward longitudinal center of the vehicle).")]
        [Range(-8f, 8f)]
        [SerializeField]
        [ShowInTelemetry]
        private float _toeAngle;

        [SerializeField]
        private List<WheelComponent> wheels = new List<WheelComponent>();
        private int _wheelCount = 2;
        private float _camber;
        private VehicleController _vc;


        public float ToeAngle
        {
            get { return _toeAngle; }
            set
            {
                _toeAngle = value;

                if (_vc != null)
                {
                    ApplyGeometryValues();
                }
            }
        }

        public float CasterAngle
        {
            get { return _casterAngle; }
            set
            {
                _casterAngle = value;

                if (_vc != null)
                {
                    ApplyGeometryValues();
                }
            }
        }


        public WheelComponent LeftWheel
        {
            get { return wheels.Count == 0 ? null : wheels[0]; }
        }

        public WheelComponent RightWheel
        {
            get { return wheels.Count <= 1 ? null : wheels[1]; }
        }

        public WheelComponent Wheel
        {
            get { return wheels.Count == 0 ? null : wheels[0]; }
        }

        public List<WheelComponent> Wheels
        {
            get { return wheels; }
        }


        public void Initialize(VehicleController vc)
        {
            _vc = vc;

            FindBelongingWheels();
            _wheelCount = wheels.Count;
            ApplyGeometryValues();
        }


        public void FindBelongingWheels()
        {
            if (_vc == null)
            {
                Debug.LogError("VehicleController is null.");
                return;
            }

            int groupIndex = _vc.powertrain.wheelGroups.IndexOf(this);
            wheels.Clear();

            foreach (WheelComponent wheel in FindWheelsBelongingToGroup(ref _vc.powertrain.wheels, groupIndex))
            {
                AddWheel(wheel);
            }

            if (wheels.Count == 2)
            {
                trackWidth = Vector3.Distance(
                    LeftWheel.wheelUAPI.transform.position,
                    RightWheel.wheelUAPI.transform.position);
            }
        }

        public void ManualUpdate()
        {
            // Calculate and set solid axle camber
            if (isSolid && _wheelCount == 2 && trackWidth != 0)
            {
                WheelComponent leftWheel = wheels[0];
                WheelComponent rightWheel = wheels[1];

                float s0 = leftWheel.wheelUAPI.SpringLength;
                float s1 = rightWheel.wheelUAPI.SpringLength;

                _camber = Mathf.Atan2(s1 - s0, trackWidth) * Mathf.Rad2Deg;

                // Set camber
                float negativeCamber = -_camber;
                leftWheel.wheelUAPI.CamberTop = negativeCamber;
                leftWheel.wheelUAPI.CamberBottom = negativeCamber;

                rightWheel.wheelUAPI.CamberTop = _camber;
                rightWheel.wheelUAPI.CamberBottom = _camber;
            }
        }


        public void ApplyGeometryValues()
        {
            foreach (WheelComponent wheel in Wheels)
            {
                if (wheel.wheelUAPI.transform.localPosition.x >= 0)
                {
                    wheel.wheelUAPI.transform.localEulerAngles = new Vector3(
                        -CasterAngle,
                        -ToeAngle,
                        wheel.wheelUAPI.transform.localEulerAngles.z);
                }
                else
                {
                    wheel.wheelUAPI.transform.localEulerAngles = new Vector3(
                        -CasterAngle,
                        ToeAngle,
                        wheel.wheelUAPI.transform.localEulerAngles.z);
                }
            }
        }


        public void AddWheel(WheelComponent wheel)
        {
            Wheels.Add(wheel);
            wheel.wheelGroup = this;
        }


        public List<WheelComponent> FindWheelsBelongingToGroup(ref List<WheelComponent> wheels, int thisGroupIndex)
        {
            List<WheelComponent> belongingWheels = new List<WheelComponent>();
            foreach (WheelComponent wheelComponent in wheels)
            {
                if (wheelComponent.wheelGroupSelector.index == thisGroupIndex)
                {
                    belongingWheels.Add(wheelComponent);
                }
            }

            return belongingWheels;
        }


        public void RemoveWheel(WheelComponent wheel)
        {
            Wheels.Remove(wheel);
        }


        public void SetWheels(List<WheelComponent> wheels)
        {
            this.wheels = wheels;
            foreach (WheelComponent wheelComponent in wheels)
            {
                wheelComponent.wheelGroup = this;
            }
        }

    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Powertrain.Wheel
{
    [CustomPropertyDrawer(typeof(WheelGroup))]
    public partial class WheelGroupDrawer : NVP_NUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            WheelGroup wheelGroup = SerializedPropertyHelper.GetTargetObjectOfProperty(property) as WheelGroup;

            drawer.BeginSubsection("General");
            drawer.Field("name");
            drawer.EndSubsection();

            drawer.BeginSubsection("Steering");
            drawer.Field("steerCoefficient");
            drawer.Field("addAckerman");
            drawer.EndSubsection();

            drawer.BeginSubsection("Brakes");
            drawer.Field("brakeCoefficient");
            drawer.Field("handbrakeCoefficient");
            drawer.EndSubsection();

            drawer.BeginSubsection("Geometry");
            drawer.Field("_toeAngle", true, "deg");
            drawer.Field("_casterAngle", true, "deg");
            if (Application.isPlaying)
            {
                if (drawer.Button("Apply Geometry"))
                {
                    wheelGroup.ApplyGeometryValues();
                }
            }
            drawer.EndSubsection();

            drawer.BeginSubsection("Axle");
            drawer.Info("Anti-roll Bar force was removed in favor of WheelController 'Force App. Point Distance'. The effect is very similar but the latter " +
                "is better for lower physics update rate applications and does not cause jitter.");
            drawer.Space(10);
            drawer.Field("isSolid");
            drawer.Info(
                "Field 'Axle Is Solid' will only work if wheel group has two wheels - a left and a right one.");
            drawer.EndSubsection();


            drawer.EndProperty();
            return true;
        }
    }
}

#endif
