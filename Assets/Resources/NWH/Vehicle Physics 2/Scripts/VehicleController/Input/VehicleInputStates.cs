using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Input
{
    /// <summary>
    ///     Struct for storing input states of the vehicle.
    ///     Allows for input to be copied between the vehicles.
    /// </summary>
    [Serializable]
    public struct VehicleInputStates
    {
        [Range(-1f, 1f)]
        [NonSerialized]
        public float steering;

        [Range(0, 1f)]
        [NonSerialized]
        public float throttle;

        [Range(0, 1f)]
        [NonSerialized]
        public float brakes;

        [Range(0f, 1f)]
        [NonSerialized]
        public float clutch;

        [NonSerialized]
        public bool engineStartStop;

        [NonSerialized]
        public bool extraLights;

        [NonSerialized]
        public bool highBeamLights;

        [Range(0f, 1f)]
        [NonSerialized]
        public float handbrake;

        [NonSerialized]
        public bool hazardLights;

        [NonSerialized]
        public bool horn;

        [NonSerialized]
        public bool leftBlinker;

        [NonSerialized]
        public bool lowBeamLights;

        [NonSerialized]
        public bool rightBlinker;

        [NonSerialized]
        public bool shiftDown;

        [NonSerialized]
        public int shiftInto;

        [NonSerialized]
        public bool shiftUp;

        [NonSerialized]
        public bool trailerAttachDetach;

        [NonSerialized]
        public bool cruiseControl;

        [NonSerialized]
        public bool boost;

        [NonSerialized]
        public bool flipOver;



        public void Reset()
        {
            steering = 0;
            throttle = 0;
            clutch = 0;
            handbrake = 0;
            shiftUp = false;
            shiftDown = false;
            shiftInto = -999;
            leftBlinker = false;
            rightBlinker = false;
            lowBeamLights = false;
            highBeamLights = false;
            hazardLights = false;
            extraLights = false;
            trailerAttachDetach = false;
            horn = false;
            engineStartStop = false;
            cruiseControl = false;
            boost = false;
            flipOver = false;
        }
    }
}