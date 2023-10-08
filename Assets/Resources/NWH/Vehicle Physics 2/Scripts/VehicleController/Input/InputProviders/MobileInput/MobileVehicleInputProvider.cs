using NWH.Common.Input;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Input
{
    /// <summary>
    ///     Class for handling mobile user input via touch screen and sensors.
    /// </summary>
    [RequireComponent(typeof(MobileSceneInputProvider))]
    public partial class MobileVehicleInputProvider : VehicleInputProviderBase
    {
        /// <summary>
        ///     Steer input device.
        ///     Accelerometer - uses sensors to get horizontal axis.
        ///     Steering Wheel - uses SteeringWheel script and steering wheel on-screen graphic that can be rotated by dragging.
        ///     Button - uses button to get horizontal axis.
        /// </summary>
        public enum HorizontalAxisType
        {
            None,
            Accelerometer,
            SteeringWheel,
            Button,
            Screen,
        }

        public enum VerticalAxisType
        {
            None,
            Accelerometer,
            Button,
            Screen,
        }

        public MobileInputButton boostButton;
        public MobileInputButton brakeButton;
        public MobileInputButton cruiseControlButton;
        public MobileInputButton engineStartStopButton;
        public MobileInputButton extraLightsButton;
        public MobileInputButton flipOverButton;
        public MobileInputButton handbrakeButton;
        public MobileInputButton hazardLightsButton;
        public MobileInputButton highBeamLightsButton;
        public MobileInputButton hornButton;
        public MobileInputButton leftBlinkerButton;
        public MobileInputButton lowBeamLightsButton;
        public MobileInputButton rightBlinkerButton;
        public MobileInputButton shiftDownButton;
        public MobileInputButton shiftUpButton;
        public MobileInputButton steerLeftButton;
        public MobileInputButton steerRightButton;
        public MobileInputButton throttleButton;

        /// <summary>
        ///     Active steer devices.
        /// </summary>
        [FormerlySerializedAs("horizontalInputType")]
        [Tooltip("    Active steer devices.")]
        public HorizontalAxisType steeringInputType = HorizontalAxisType.SteeringWheel;


        /// <summary>
        ///     Steering wheel script. Optional and not needed if SteeringWheel option is not used.
        /// </summary>
        [Tooltip("    Steering wheel script. Optional and not needed if SteeringWheel option is not used.")]
        public SteeringWheel steeringWheel;


        /// <summary>
        ///     Higher value will result in higher steer angle for same tilt.
        /// </summary>
        [Tooltip("    Higher value will result in higher steer angle for same tilt.")]
        public float tiltSensitivity = 1.5f;

        public MobileInputButton trailerAttachDetachButton;

        /// <summary>
        ///     Active steer devices.
        /// </summary>
        [Tooltip("    Active steer devices.")]
        public VerticalAxisType verticalInputType = VerticalAxisType.Button;


        public override bool EngineStartStop()
        {
            return engineStartStopButton != null && engineStartStopButton.hasBeenClicked;
        }


        public override float Clutch()
        {
            // Not implemented
            return 0f;
        }


        public override bool ExtraLights()
        {
            return extraLightsButton != null && extraLightsButton.hasBeenClicked;
        }


        public override bool HighBeamLights()
        {
            return highBeamLightsButton != null && highBeamLightsButton.hasBeenClicked;
        }


        public override float Handbrake()
        {
            return handbrakeButton == null ? 0 : handbrakeButton.isPressed ? 1 : 0;
        }


        public override bool HazardLights()
        {
            return hazardLightsButton != null && hazardLightsButton.hasBeenClicked;
        }


        public override float Steering()
        {
            // Steering wheel input
            if (steeringInputType == HorizontalAxisType.SteeringWheel)
            {
                if (steeringWheel != null)
                {
                    return steeringWheel.GetClampedValue();
                }

                Debug.LogWarning("HorizontalAxisType is set to SteeringWheel but no Steering Wheel has been assigned.");
            }
            // Accelerometer input
            else if (steeringInputType == HorizontalAxisType.Accelerometer)
            {
                return UnityEngine.Input.acceleration.x * tiltSensitivity;
            }
            // Button input
            else if (steeringInputType == HorizontalAxisType.Button)
            {
                if (steerLeftButton != null && steerRightButton != null)
                {
                    return steerLeftButton.isPressed ? -1f : steerRightButton.isPressed ? 1f : 0f;
                }

                Debug.LogWarning("HorizontalAxisType is set to button but buttons have not been assigned.");
                return 0f;
            }

            return 0;
        }


        public override bool Horn()
        {
            return hornButton != null && hornButton.isPressed;
        }


        public override bool LeftBlinker()
        {
            return leftBlinkerButton != null && leftBlinkerButton.hasBeenClicked;
        }


        public override bool LowBeamLights()
        {
            return lowBeamLightsButton != null && lowBeamLightsButton.hasBeenClicked;
        }


        public override bool RightBlinker()
        {
            return rightBlinkerButton != null && rightBlinkerButton.hasBeenClicked;
        }


        public override bool ShiftDown()
        {
            return shiftDownButton != null && shiftDownButton.hasBeenClicked;
        }


        public override int ShiftInto()
        {
            // Not implemented
            return -999;
        }


        public override bool ShiftUp()
        {
            return shiftUpButton != null && shiftUpButton.hasBeenClicked;
        }


        public override bool TrailerAttachDetach()
        {
            return trailerAttachDetachButton != null && trailerAttachDetachButton.hasBeenClicked;
        }


        public override float Throttle()
        {
            // Accelerometer input
            if (verticalInputType == VerticalAxisType.Accelerometer)
            {
                return Mathf.Clamp01(UnityEngine.Input.acceleration.y * tiltSensitivity);
            }

            // Button input
            if (verticalInputType == VerticalAxisType.Button)
            {
                if (throttleButton != null)
                {
                    return throttleButton.isPressed ? 1f : 0f;
                }

                Debug.LogWarning("VerticalAxisType is set to button but buttons have not been assigned.");
                return 0f;
            }

            return 0;
        }


        public override float Brakes()
        {
            // Accelerometer input
            if (verticalInputType == VerticalAxisType.Accelerometer)
            {
                return Mathf.Clamp01(-UnityEngine.Input.acceleration.y * tiltSensitivity);
            }

            // Button input
            if (verticalInputType == VerticalAxisType.Button)
            {
                if (brakeButton != null)
                {
                    return brakeButton.isPressed ? 1f : 0f;
                }

                Debug.LogWarning("VerticalAxisType is set to button but buttons have not been assigned.");
                return 0f;
            }

            return 0;
        }


        public override bool FlipOver()
        {
            return flipOverButton != null && flipOverButton.hasBeenClicked;
        }


        public override bool Boost()
        {
            return boostButton != null && boostButton.isPressed;
        }


        public override bool CruiseControl()
        {
            return cruiseControlButton != null && cruiseControlButton.hasBeenClicked;
        }
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Input
{
    /// <summary>
    ///     Editor for MobileInputProvider.
    /// </summary>
    [CustomEditor(typeof(MobileVehicleInputProvider))]
    public partial class MobileVehicleInputProviderEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            drawer.Info("None of the buttons are mandatory. If you do not wish to use an input leave the field empty.");

            MobileVehicleInputProvider mip = target as MobileVehicleInputProvider;
            if (mip == null)
            {
                drawer.EndEditor(this);
                return false;
            }

            drawer.BeginSubsection("Input Type");
            drawer.Field("steeringInputType");
            drawer.Field("verticalInputType");
            drawer.EndSubsection();

            drawer.BeginSubsection("Direction");
            if (mip.steeringInputType == MobileVehicleInputProvider.HorizontalAxisType.SteeringWheel)
            {
                drawer.Field("steeringWheel");
            }

            if (mip.steeringInputType == MobileVehicleInputProvider.HorizontalAxisType.Accelerometer ||
                mip.verticalInputType == MobileVehicleInputProvider.VerticalAxisType.Accelerometer)
            {
                drawer.Field("tiltSensitivity");
            }

            if (mip.steeringInputType == MobileVehicleInputProvider.HorizontalAxisType.Button)
            {
                drawer.Field("steerLeftButton");
                drawer.Field("steerRightButton");
            }

            if (mip.verticalInputType == MobileVehicleInputProvider.VerticalAxisType.Button)
            {
                drawer.Field("throttleButton");
                drawer.Field("brakeButton");
            }

            drawer.EndSubsection();

            drawer.BeginSubsection("Scene Buttons");
            drawer.Info("Since v1.2 scene buttons are assigned through MobileSceneInputProvider component");
            drawer.EndSubsection();

            drawer.BeginSubsection("Vehicle Buttons");
            drawer.Field("engineStartStopButton");
            drawer.Field("handbrakeButton");
            drawer.Field("shiftUpButton");
            drawer.Field("shiftDownButton");
            drawer.Field("extraLightsButton");
            drawer.Field("highBeamLightsButton");
            drawer.Field("lowBeamLightsButton");
            drawer.Field("hazardLightsButton");
            drawer.Field("leftBlinkerButton");
            drawer.Field("rightBlinkerButton");
            drawer.Field("hornButton");
            drawer.Field("trailerAttachDetachButton");
            drawer.Field("boostButton");
            drawer.EndSubsection();

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
