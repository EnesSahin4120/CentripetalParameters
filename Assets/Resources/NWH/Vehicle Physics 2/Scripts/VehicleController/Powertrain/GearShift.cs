using UnityEngine;

namespace NWH.VehiclePhysics2.Powertrain
{
    /// <summary>
    ///     Class that stores gear shift data.
    /// </summary>
    public partial class GearShift
    {
        /// <summary>
        ///     Duration of the current gear shift.
        /// </summary>
        public float Duration { get; private set; }

        /// <summary>
        ///     Gear from which the gear shift starts.
        /// </summary>
        public int FromGear { get; private set; }

        /// <summary>
        ///     Target gear.
        /// </summary>
        public int ToGear { get; private set; }

        /// <summary>
        ///     StartEngine time of a gear shift.
        /// </summary>
        public float StartTime { get; private set; }

        /// <summary>
        ///     End time of a gear shift. Calculated from start time and duration.
        /// </summary>
        public float EndTime
        {
            get { return StartTime + Duration; }
        }

        /// <summary>
        ///     Has the gear shift ended? If true the GearShift is not valid anymore.
        /// </summary>
        public bool HasEnded
        {
            get { return Time.realtimeSinceStartup > EndTime; }
        }

        /// <summary>
        ///     True when shifting to a larger gear. Works in reverse too.
        ///     Example: 2 to 3, -2 to -3.
        /// </summary>
        public bool IsUpshift
        {
            get { return Mathf.Abs(FromGear) < Mathf.Abs(ToGear); }
        }

        /// <summary>
        ///     Opposite of IsUpshift.
        /// </summary>
        public bool IsDownshift
        {
            get { return !IsUpshift; }
        }

        /// <summary>
        ///     Engine RPM at the start of the shift.
        /// </summary>
        public float FromRpm { get; private set; }

        /// <summary>
        ///     Ideal engine RPM at the end of the shift.
        ///     -1 if either ToGear of FromGear is 0.
        /// </summary>
        public float ToRpm { get; private set; }


        public void RegisterShift(float currentTime, float duration, int toGear, int fromGear, float fromRPM,
            float toRPM)
        {
            StartTime = currentTime;
            Duration = duration;
            ToGear = toGear;
            FromGear = fromGear;
            FromRpm = fromRPM;
            ToRpm = toRPM;
            //Debug.Log($"Shifting from {_fromGear} to {_toGear} in {_duration}s.");
        }
    }
}