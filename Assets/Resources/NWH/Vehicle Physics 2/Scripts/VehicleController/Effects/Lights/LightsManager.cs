using NWH.Common.Vehicles;
using System;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Effects
{
    /// <summary>
    ///     Class for controlling all of the vehicle lights.
    /// </summary>
    [Serializable]
    public partial class LightsMananger : Effect
    {
        /// <summary>
        ///     Rear lights that will light up when brake is pressed. Always red.
        /// </summary>
        [FormerlySerializedAs("stopLights")]
        [Tooltip("    Rear lights that will light up when brake is pressed. Always red.")]
        public VehicleLight brakeLights = new VehicleLight();

        /// <summary>
        ///     Can be used for any type of special lights, e.g. beacons.
        /// </summary>
        [Tooltip("    Can be used for any type of special lights, e.g. beacons.")]
        public VehicleLight extraLights = new VehicleLight();

        /// <summary>
        ///     High (full) beam lights.
        /// </summary>
        [FormerlySerializedAs("fullBeams")]
        [Tooltip("    High (full) beam lights.")]
        public VehicleLight highBeamLights = new VehicleLight();

        /// <summary>
        ///     Blinkers on the left side of the vehicle.
        /// </summary>
        [Tooltip("    Blinkers on the left side of the vehicle.")]
        public VehicleLight leftBlinkers = new VehicleLight();

        /// <summary>
        ///     Low beam lights.
        /// </summary>
        [FormerlySerializedAs("headLights")]
        [Tooltip("    Low beam lights.")]
        public VehicleLight lowBeamLights = new VehicleLight();

        /// <summary>
        ///     Rear Lights that will light up when vehicle is in reverse gear(s). Usually white.
        /// </summary>
        [Tooltip("    Rear Lights that will light up when vehicle is in reverse gear(s). Usually white.")]
        public VehicleLight reverseLights = new VehicleLight();

        /// <summary>
        ///     Blinkers on the right side of the vehicle.
        /// </summary>
        [Tooltip("    Blinkers on the right side of the vehicle.")]
        public VehicleLight rightBlinkers = new VehicleLight();

        /// <summary>
        ///     Rear Lights that will light up when headlights are on. Always red.
        /// </summary>
        [FormerlySerializedAs("rearLights")]
        [Tooltip("    Rear Lights that will light up when headlights are on. Always red.")]
        public VehicleLight tailLights = new VehicleLight();

        private bool _hazardLightsOn;
        private bool _leftBlinkersOn;
        private bool _rightBlinkersOn;
        private float _leftBlinkerTurnOnTime;
        private float _rightBlinkerTurnOnTime;

        public bool LeftBlinkerState
        {
            get { return (int)((vc.realtimeSinceStartup - _leftBlinkerTurnOnTime) * 2) % 2 == 0; }
        }

        public bool RightBlinkerState
        {
            get { return (int)((vc.realtimeSinceStartup - _rightBlinkerTurnOnTime) * 2) % 2 == 0; }
        }


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            if (IsEnabled && !vc.GetMultiplayerIsRemote())
            {
                // Brake lights
                if (brakeLights != null)
                {
                    if (vc.brakes.IsBraking)
                    {
                        brakeLights.TurnOn();
                    }
                    else
                    {
                        bool tailLightsOn = tailLights.On;
                        brakeLights.TurnOff();
                        if (tailLightsOn)
                        {
                            tailLights.TurnOff();
                            tailLights.TurnOn();
                        }
                    }
                }

                // Reversing lights
                if (reverseLights != null)
                {
                    if (vc.powertrain.transmission.Gear < 0)
                    {
                        reverseLights.TurnOn();
                    }
                    else
                    {
                        reverseLights.TurnOff();
                    }
                }

                // Low beam lights
                if (lowBeamLights != null)
                {
                    if (vc.input.states.lowBeamLights)
                    {
                        lowBeamLights.Toggle();
                        if (lowBeamLights.On)
                        {
                            tailLights.TurnOn();
                        }
                        else
                        {
                            if (brakeLights != null)
                            {
                                bool brakeLightsOn = brakeLights.On;
                                tailLights.TurnOff();
                                if (brakeLightsOn)
                                {
                                    brakeLights.TurnOff();
                                    brakeLights.TurnOn();
                                }
                            }
                            else
                            {
                                tailLights.TurnOff();
                            }

                            if (highBeamLights != null)
                            {
                                highBeamLights.TurnOff();
                            }
                        }

                        vc.input.states.lowBeamLights = false;
                    }
                }

                // High beam lights
                if (highBeamLights != null && lowBeamLights != null)
                {
                    if (vc.input.states.highBeamLights)
                    {
                        bool prevState = highBeamLights.On;
                        highBeamLights.Toggle();
                        if (highBeamLights.On && !prevState)
                        {
                            lowBeamLights.TurnOn();
                            tailLights.TurnOn();
                        }
                        else if (!highBeamLights.On && !lowBeamLights.On)
                        {
                            tailLights.TurnOff();
                        }

                        vc.input.states.highBeamLights = false;
                    }
                }

                // Blinkers and hazards
                if (leftBlinkers != null && rightBlinkers != null)
                {
                    if (vc.input.states.hazardLights)
                    {
                        _hazardLightsOn = !_hazardLightsOn;
                        _leftBlinkersOn = _rightBlinkersOn = _hazardLightsOn;
                        if (_hazardLightsOn)
                        {
                            _leftBlinkerTurnOnTime = _rightBlinkerTurnOnTime = vc.realtimeSinceStartup;
                        }
                        else
                        {
                            _leftBlinkersOn = false;
                            _rightBlinkersOn = false;
                        }

                        vc.input.states.hazardLights = false;
                    }

                    if (!_hazardLightsOn)
                    {
                        if (vc.input.states.leftBlinker)
                        {
                            _leftBlinkersOn = !_leftBlinkersOn;
                            if (_leftBlinkersOn)
                            {
                                _leftBlinkerTurnOnTime = vc.realtimeSinceStartup;
                                _rightBlinkersOn = false;
                            }

                            vc.input.states.leftBlinker = false;
                        }

                        if (vc.input.states.rightBlinker)
                        {
                            _rightBlinkersOn = !_rightBlinkersOn;
                            if (_rightBlinkersOn)
                            {
                                _rightBlinkerTurnOnTime = vc.realtimeSinceStartup;
                                _leftBlinkersOn = false;
                            }

                            vc.input.states.rightBlinker = false;
                        }
                    }
                    else
                    {
                        vc.input.states.leftBlinker = false;
                        vc.input.states.rightBlinker = false;
                    }

                    leftBlinkers.SetState(_leftBlinkersOn && LeftBlinkerState);
                    rightBlinkers.SetState(_rightBlinkersOn && RightBlinkerState);
                }

                // Extra lights
                if (extraLights != null)
                {
                    if (vc.input.states.extraLights)
                    {
                        extraLights.Toggle();
                        vc.input.states.extraLights = false;
                    }
                }
            }
        }


        public override void Disable()
        {
            base.Disable();

            TurnOffAllLights();
        }


        /// <summary>
        ///     Returns light states as a int with each bit representing one light;
        /// </summary>
        public int GetIntState()
        {
            int intState = 0;

            SetBit(ref intState, brakeLights.On, 0);
            SetBit(ref intState, tailLights.On, 1);
            SetBit(ref intState, reverseLights.On, 2);
            SetBit(ref intState, lowBeamLights.On, 3);
            SetBit(ref intState, highBeamLights.On, 4);
            SetBit(ref intState, leftBlinkers.On, 5);
            SetBit(ref intState, rightBlinkers.On, 6);
            SetBit(ref intState, extraLights.On, 7);

            return intState;
        }


        /// <summary>
        ///     Sets state of lights from a single int where each bit represents one light.
        ///     To be used with GetIntState().
        /// </summary>
        public void SetStateFromInt(int intState)
        {
            brakeLights.SetState(GetBit(intState, 0));
            tailLights.SetState(GetBit(intState, 1));
            reverseLights.SetState(GetBit(intState, 2));
            lowBeamLights.SetState(GetBit(intState, 3));
            highBeamLights.SetState(GetBit(intState, 4));
            leftBlinkers.SetState(GetBit(intState, 5));
            rightBlinkers.SetState(GetBit(intState, 6));
            extraLights.SetState(GetBit(intState, 7));
        }


        private void SetBit(ref int target, bool value, int position)
        {
            if (value)
            {
                target |= 1 << position;
            }
            else
            {
                target &= ~(1 << position);
            }
        }


        private bool GetBit(int source, int position)
        {
            return ((source >> position) & 1) == 1;
        }


        /// <summary>
        ///     Turns off all lights and emission on all meshes.
        /// </summary>
        public void TurnOffAllLights()
        {
            brakeLights.TurnOff();
            lowBeamLights.TurnOff();
            tailLights.TurnOff();
            reverseLights.TurnOff();
            highBeamLights.TurnOff();
            leftBlinkers.TurnOff();
            rightBlinkers.TurnOff();
            extraLights.TurnOff();
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Effects
{
    [CustomPropertyDrawer(typeof(LightsMananger))]
    public partial class LightsManagerDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            int menuIndex =
                drawer.HorizontalToolbar("LightsMenu", new[] { "Front", "Rear", "Blinkers", "Misc", }, true, true);

            if (menuIndex == 0)
            {
                drawer.Field("lowBeamLights");
                drawer.Field("highBeamLights");
            }
            else if (menuIndex == 1)
            {
                drawer.Field("tailLights");
                drawer.Field("brakeLights");
                drawer.Field("reverseLights");
            }
            else if (menuIndex == 2)
            {
                drawer.Field("leftBlinkers");
                drawer.Field("rightBlinkers");
            }
            else
            {
                drawer.Field("extraLights");
            }

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
