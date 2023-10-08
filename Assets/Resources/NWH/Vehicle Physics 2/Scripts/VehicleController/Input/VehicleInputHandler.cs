using System;
using System.Collections.Generic;
using NWH.Common.Input;
using UnityEngine;
using UnityEngine.Serialization;
using NWH.Common.Vehicles;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Input
{
    /// <summary>
    ///     Manages vehicle input by retrieving it from the active InputProvider and filling in the InputStates with the
    ///     fetched data.
    /// </summary>
    [Serializable]
    public partial class VehicleInputHandler : VehicleComponent
    {
        /// <summary>
        ///     When enabled input will be auto-retrieved from the InputProviders present in the scene.
        ///     Disable to manualy set the input through external scripts, i.e. AI controller.
        /// </summary>
        [FormerlySerializedAs("autoSettable")]
        [UnityEngine.Tooltip("    When enabled input will be auto-retrieved from the InputProviders present in the scene.\r\n    Disable to manualy set the input through external scripts, i.e. AI controller.")]
        public bool autoSetInput = true;

        /// <summary>
        ///     All the input states of the vehicle. Can be used to set input through scripting or copy the inputs
        ///     over from other vehicle, such as truck to trailer.
        /// </summary>
        [Tooltip(
            "All the input states of the vehicle. Can be used to set input through scripting or copy the inputs\r\nover from other vehicle, such as truck to trailer.")]
        [NonSerialized] public VehicleInputStates states;

        /// <summary>
        ///     Swaps throttle and brake axes when vehicle is in reverse.
        /// </summary>
        [Tooltip("    Swaps throttle and brake axes when vehicle is in reverse.")]
        [ShowInSettings("Swap Input In R")]
        public bool swapInputInReverse = true;

        /// <summary>
        ///     List of scene input providers.
        /// </summary>
        private List<InputProvider> _inputProviders = new List<InputProvider>();

        /// <summary>
        ///     Convenience function for setting throttle/brakes as a single value.
        ///     Use Throttle/Brake axes to apply throttle and braking separately.
        ///     If the set value is larger than 0 throttle will be set, else if value is less than 0 brake axis will be set.
        /// </summary>
        public float Vertical
        {
            get { return states.throttle - states.brakes; }
            set
            {
                float clampedValue = value < -1 ? -1 : value > 1 ? 1 : value;

                if (value > 0)
                {
                    states.throttle = clampedValue;
                    states.brakes = 0;
                }
                else
                {
                    states.throttle = 0;
                    states.brakes = -clampedValue;
                }
            }
        }

        /// <summary>
        ///     Throttle axis.
        ///     For combined throttle/brake input (such as prior to v1.0.1) use 'Vertical' instead.
        /// </summary>
        public float Throttle
        {
            get { return states.throttle; }
            set
            {
                value = value < 0f ? 0f : value > 1f ? 1f : value;
                states.throttle = value;
            }
        }

        /// <summary>
        ///     Brake axis.
        ///     For combined throttle/brake input use 'Vertical' instead.
        /// </summary>
        public float Brakes
        {
            get { return states.brakes; }
            set
            {
                value = value < 0f ? 0f : value > 1f ? 1f : value;
                states.brakes = value;
            }
        }

        /// <summary>
        ///     Returns throttle or brake input based on 'swapInputInReverse' setting and current gear.
        ///     If swapInputInReverse is true, brake will act as throttle and vice versa while driving in reverse.
        /// </summary>
        public float InputSwappedThrottle
        {
            get { return IsInputSwapped ? Brakes : Throttle; }
        }

        /// <summary>
        ///     Returns throttle or brake input based on 'swapInputInReverse' setting and current gear.
        ///     If swapInputInReverse is true, throttle will act as brake and vise versa while driving in reverse.
        /// </summary>
        public float InputSwappedBrakes
        {
            get { return IsInputSwapped ? Throttle : Brakes; }
        }

        /// <summary>
        ///     Steering axis.
        /// </summary>
        public float Steering
        {
            get { return states.steering; }
            set
            {
                value = value < -1f ? -1f : value > 1f ? 1f : value;
                states.steering = value;
            }
        }

        /// <summary>
        ///     Clutch axis.
        /// </summary>
        public float Clutch
        {
            get { return states.clutch; }
            set
            {
                value = value < 0f ? 0f : value > 1f ? 1f : value;
                states.clutch = value;
            }
        }

        public bool EngineStartStop
        {
            get { return states.engineStartStop; }
            set { states.engineStartStop = value; }
        }

        public bool ExtraLights
        {
            get { return states.extraLights; }
            set { states.extraLights = value; }
        }

        public bool HighBeamLights
        {
            get { return states.highBeamLights; }
            set { states.highBeamLights = value; }
        }

        public float Handbrake
        {
            get { return states.handbrake; }
            set
            {
                value = value < 0f ? 0f : value > 1f ? 1f : value;
                states.handbrake = value;
            }
        }

        public bool HazardLights
        {
            get { return states.hazardLights; }
            set { states.hazardLights = value; }
        }

        public bool Horn
        {
            get { return states.horn; }
            set { states.horn = value; }
        }

        public bool LeftBlinker
        {
            get { return states.leftBlinker; }
            set { states.leftBlinker = value; }
        }

        public bool LowBeamLights
        {
            get { return states.lowBeamLights; }
            set { states.lowBeamLights = value; }
        }

        public bool RightBlinker
        {
            get { return states.rightBlinker; }
            set { states.rightBlinker = value; }
        }

        public bool ShiftDown
        {
            get { return states.shiftDown; }
            set { states.shiftDown = value; }
        }

        public int ShiftInto
        {
            get { return states.shiftInto; }
            set { states.shiftInto = value; }
        }

        public bool ShiftUp
        {
            get { return states.shiftUp; }
            set { states.shiftUp = value; }
        }

        public bool TrailerAttachDetach
        {
            get { return states.trailerAttachDetach; }
            set { states.trailerAttachDetach = value; }
        }

        public bool CruiseControl
        {
            get { return states.cruiseControl; }
            set { states.cruiseControl = value; }
        }

        public bool Boost
        {
            get { return states.boost; }
            set { states.boost = value; }
        }

        public bool FlipOver
        {
            get { return states.flipOver; }
            set { states.flipOver = value; }
        }

        /// <summary>
        ///     True when throttle and brake axis are swapped.
        /// </summary>
        public bool IsInputSwapped
        {
            get { return swapInputInReverse && vc.powertrain.transmission.IsInReverse; }
        }


        public override void Initialize()
        {
            _inputProviders = InputProvider.Instances;

            if (autoSetInput && (_inputProviders == null || _inputProviders.Count == 0))
            {
                Debug.LogWarning(
                    "No InputProviders are present in the scene. " +
                    "Make sure that one or more InputProviders are present (DesktopInputProvider, MobileInputProvider, etc.).");
            }

            states.Reset(); // Reset states to make sure that initial values are neutral in case the behaviour was copied or similar.

            base.Initialize();
        }


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            if (!autoSetInput)
            {
                return;
            }


            Throttle = InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.Throttle());
            Brakes = InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.Brakes());

            Steering = InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.Steering());
            Clutch = InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.Clutch());
            Handbrake = InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.Handbrake());
            ShiftInto = CombinedInputGear<VehicleInputProviderBase>(i => i.ShiftInto());

            ShiftUp |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.ShiftUp());
            ShiftDown |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.ShiftDown());

            LeftBlinker |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.LeftBlinker());
            RightBlinker |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.RightBlinker());
            LowBeamLights |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.LowBeamLights());
            HighBeamLights |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.HighBeamLights());
            HazardLights |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.HazardLights());
            ExtraLights |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.ExtraLights());

            Horn = InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.Horn());
            EngineStartStop |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.EngineStartStop());

            Boost = InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.Boost());
            TrailerAttachDetach = TrailerAttachDetach ||
                                  InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.TrailerAttachDetach());
            CruiseControl |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.CruiseControl());
            FlipOver = FlipOver || InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.FlipOver());
        }


        public override void Disable()
        {
            base.Disable();
            states.Reset();
        }


        public static int CombinedInputGear<T>(Func<T, int> selector) where T : InputProvider
        {
            int gear = -999;
            for (int i = 0; i < InputProvider.Instances.Count; i++)
            {
                InputProvider ip = InputProvider.Instances[i];
                if (ip is T)
                {
                    int tmp = selector(ip as T);
                    if (tmp > gear)
                    {
                        gear = tmp;
                    }
                }
            }

            return gear;
        }


        public void ResetShiftFlags()
        {
            states.shiftUp = false;
            states.shiftDown = false;
            states.shiftInto = -999;
        }


        private delegate bool BinaryInputDelegate();
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Input
{
    /// <summary>
    ///     Property drawer for Input.
    /// </summary>
    [CustomPropertyDrawer(typeof(VehicleInputHandler))]
    public partial class VehicleInputHandlerDrawer : ComponentNUIPropertyDrawer
    {
        private float infoHeight;


        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("autoSetInput");
            drawer.Info(
                "While autoSetInput is set to 'true' vehicle will automatically get input from all available InputProviders in this " +
                "scene. To disable this behaviour untick the option. While this option is active any input set from external scripts will get" +
                " overridden.");
            drawer.Field("swapInputInReverse");

            drawer.BeginSubsection("Input States");
            VehicleController vc = property.serializedObject.targetObject as VehicleController;
            if (Application.isPlaying && vc != null && vc.input != null)
            {
                VehicleInputStates vis = vc.input.states;

                drawer.Label($"Steering: {vis.steering}");
                drawer.Label($"Throttle: {vis.throttle}");
                drawer.Label($"Brakes: {vis.brakes}");
                drawer.Label($"Clutch: {vis.clutch}");
                drawer.Label($"Handbrake: {vis.handbrake}");
                drawer.Label($"ShiftUp: {vis.shiftUp}");
                drawer.Label($"ShiftDown: {vis.shiftDown}");
                drawer.Label($"ShiftInto: {vis.shiftInto}");
                drawer.Label($"LeftBlinker: {vis.leftBlinker}");
                drawer.Label($"RightBlinker: {vis.rightBlinker}");
                drawer.Label($"LowBeamLights: {vis.lowBeamLights}");
                drawer.Label($"HighBeamLights: {vis.highBeamLights}");
                drawer.Label($"HazardLights: {vis.hazardLights}");
                drawer.Label($"ExtraLights: {vis.extraLights}");
                drawer.Label($"TrailerAttachDetach: {vis.trailerAttachDetach}");
                drawer.Label($"Horn: {vis.horn}");
                drawer.Label($"EngineStartStop: {vis.engineStartStop}");
                drawer.Label($"CruiseControl: {vis.cruiseControl}");
                drawer.Label($"Boost: {vis.boost}");
                drawer.Label($"FlipOver: {vis.flipOver}");
            }
            else
            {
                drawer.Info("States are visible in play mode only.");
            }
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
