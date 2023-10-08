using System;
using NWH.Common.Input;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.Input
{
    /// <summary>
    ///     Class for handling desktop user input via mouse and keyboard through InputManager.
    /// </summary>
    public partial class InputManagerVehicleInputProvider : VehicleInputProviderBase
    {
        /// <summary>
        ///     Should mouse be used for input?
        /// </summary>
        [Tooltip("    Should mouse be used for input?")]
        public bool mouseInput;

        /// <summary>
        ///     Names of input bindings for each individual gears. If you need to add more gears modify this and the corresponding
        ///     iterator in the
        ///     ShiftInto() function.
        /// </summary>
        [NonSerialized]
        [Tooltip(
            "Names of input bindings for each individual gears. If you need to add more gears modify this and the corresponding\r\niterator in the\r\nShiftInto() function.")]
        public string[] shiftInputNames =
        {
            "ShiftIntoR1",
            "ShiftInto0",
            "ShiftInto1",
            "ShiftInto2",
            "ShiftInto3",
            "ShiftInto4",
            "ShiftInto5",
            "ShiftInto6",
            "ShiftInto7",
            "ShiftInto8",
            "ShiftInto9",
        };

        private string _tmpStr;


        // *** VEHICLE BINDINGS ***
        public override float Steering()
        {
            float input = mouseInput
                              ? Mathf.Clamp(GetMouseHorizontal(), -1f, 1f)
                              : InputUtils.TryGetAxisRaw("Steering");
            return input;
        }


        public override float Throttle()
        {
            float input =
                mouseInput
                    ? Mathf.Clamp(GetMouseVertical(), 0f, 1f)
                    : Mathf.Clamp01(InputUtils.TryGetAxisRaw("Throttle"));
            return input;
        }


        public override float Brakes()
        {
            float input =
                mouseInput
                    ? -Mathf.Clamp(GetMouseVertical(), -1f, 0f)
                    : Mathf.Clamp01(InputUtils.TryGetAxisRaw("Brakes"));
            return input;
        }


        public override float Clutch()
        {
            float input = Mathf.Clamp01(InputUtils.TryGetAxis("Clutch"));
            return input;
        }


        public override float Handbrake()
        {
            float input = Mathf.Clamp01(InputUtils.TryGetAxis("Handbrake"));
            return input;
        }


        public override bool EngineStartStop()
        {
            return InputUtils.TryGetButtonDown("EngineStartStop", KeyCode.E);
        }


        public override bool ExtraLights()
        {
            return InputUtils.TryGetButtonDown("ExtraLights", KeyCode.Semicolon);
        }


        public override bool HighBeamLights()
        {
            return InputUtils.TryGetButtonDown("HighBeamLights", KeyCode.K);
        }


        public override bool HazardLights()
        {
            return InputUtils.TryGetButtonDown("HazardLights", KeyCode.J);
        }


        public override bool Horn()
        {
            return InputUtils.TryGetButton("Horn", KeyCode.H);
        }


        public override bool LeftBlinker()
        {
            return InputUtils.TryGetButtonDown("LeftBlinker", KeyCode.Z);
        }


        public override bool LowBeamLights()
        {
            return InputUtils.TryGetButtonDown("LowBeamLights", KeyCode.L);
        }


        public override bool RightBlinker()
        {
            return InputUtils.TryGetButtonDown("RightBlinker", KeyCode.X);
        }


        public override bool ShiftDown()
        {
            return InputUtils.TryGetButtonDown("ShiftDown", KeyCode.F);
        }


        /// <summary>
        ///     Used for H-shifters and direct shifting into gear on non-sequential gearboxes.
        /// </summary>
        public override int ShiftInto()
        {
            for (int i = -1; i < 9; i++)
            {
                if (InputUtils.TryGetButton(shiftInputNames[i + 1], KeyCode.Alpha0, false))
                {
                    return i;
                }
            }

            return -999;
        }


        public override bool ShiftUp()
        {
            return InputUtils.TryGetButtonDown("ShiftUp", KeyCode.R);
        }


        public override bool TrailerAttachDetach()
        {
            return InputUtils.TryGetButtonDown("TrailerAttachDetach", KeyCode.T);
        }


        public override bool FlipOver()
        {
            return InputUtils.TryGetButtonDown("FlipOver", KeyCode.M);
        }


        public override bool Boost()
        {
            return InputUtils.TryGetButton("Boost", KeyCode.LeftShift);
        }


        public override bool CruiseControl()
        {
            return InputUtils.TryGetButtonDown("CruiseControl", KeyCode.N);
        }


        private float GetMouseHorizontal()
        {
            float percent = Mathf.Clamp(UnityEngine.Input.mousePosition.x / Screen.width, -1f, 1f);
            if (percent < 0.5f)
            {
                return -(0.5f - percent) * 2.0f;
            }

            return (percent - 0.5f) * 2.0f;
        }


        private float GetMouseVertical()
        {
            float percent = Mathf.Clamp(UnityEngine.Input.mousePosition.y / Screen.height, -1f, 1f);
            if (percent < 0.5f)
            {
                return -(0.5f - percent) * 2.0f;
            }

            return (percent - 0.5f) * 2.0f;
        }
    }
}

#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Input
{
    [CustomEditor(typeof(InputManagerVehicleInputProvider))]
    public partial class InputManagerVehicleInputProviderEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            //drawer.Field("deadzone");
            drawer.Field("mouseInput");
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
