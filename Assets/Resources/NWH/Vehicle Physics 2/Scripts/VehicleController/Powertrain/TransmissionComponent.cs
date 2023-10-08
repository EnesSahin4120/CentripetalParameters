using System;
using System.Collections.Generic;
using NWH.Common.Utility;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using NWH.Common.Vehicles;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Powertrain
{
    [Serializable]
    public partial class TransmissionComponent : PowertrainComponent
    {
        public const float INPUT_DEADZONE = 0.04f;

        public delegate void Shift(VehicleController vc, bool shiftChecksValid);

        public delegate bool ShiftCheck();

        public enum AutomaticTransmissionReverseType
        {
            Auto,
            RequireShiftInput,
            RepeatInput,
        }

        public enum Type
        {
            Manual,
            Automatic,
            AutomaticSequential_Obsolete,
            CVT,
            External,
        }

        /// <summary>
        ///     If true the gear input has to be held for the transmission to stay in gear, otherwise it goes to neutral.
        ///     Used for hardware H-shifters.
        /// </summary>
        [Tooltip(
            "If true the gear input has to be held for the transmission to stay in gear, otherwise it goes to neutral.\r\nUsed for hardware H-shifters.")]
        public bool holdToKeepInGear;

        /// <summary>
        ///     Final gear multiplier. Each gear gets multiplied by this value.
        ///     Equivalent to axle/differential ratio in real life.
        /// </summary>
        [Tooltip(
            "    Final gear multiplier. Each gear gets multiplied by this value.\r\n    Equivalent to axle/differential ratio in real life.")]
        [ShowInSettings("Final Ratio", 1f, 20f, 1f)]
        public float finalGearRatio = 4;

        /// <summary>
        ///     Currently active gearing profile.
        ///     Final gear ratio will be determined from this and final gear ratio.
        /// </summary>
        [Tooltip(
            "    Currently active gearing profile.\r\n    Final gear ratio will be determined from this and final gear ratio.")]
        public TransmissionGearingProfile gearingProfile;

        /// <summary>
        ///     How much inclines affect shift point position. Higher value will push the shift up and shift down RPM up depending
        ///     on the current incline to prevent vehicle from upshifting at the wrong time.
        /// </summary>
        [Range(0, 4)]
        [Tooltip(
            "How much inclines affect shift point position. Higher value will push the shift up and shift down RPM up depending\r\non the current incline to prevent vehicle from upshifting at the wrong time.")]
        public float inclineEffectCoeff;

        /// <summary>
        /// Torque at which the CVT will be at its lowest gear ratio.
        /// </summary>
        [UnityEngine.Tooltip("Torque at which the CVT will be at its lowest gear ratio.")]
        public float cvtMaxInputTorque = 300f;

        /// <summary>
        ///     Event that gets triggered when transmission shifts down.
        /// </summary>
        [SerializeField]
        [Tooltip("    Event that gets triggered when transmission shifts down.")]
        public ShiftEvent onDownshift = new ShiftEvent();

        /// <summary>
        ///     Event that gets triggered when transmission shifts (up or down).
        /// </summary>
        [SerializeField]
        [Tooltip("    Event that gets triggered when transmission shifts (up or down).")]
        public ShiftEvent onShift = new ShiftEvent();

        /// <summary>
        ///     Event that gets triggered when transmission shifts up.
        /// </summary>
        [SerializeField]
        [Tooltip("    Event that gets triggered when transmission shifts up.")]
        public ShiftEvent onUpshift = new ShiftEvent();

        /// <summary>
        ///     Time after shifting in which shifting can not be done again.
        /// </summary>
        [Tooltip("    Time after shifting in which shifting can not be done again.")]
        public float postShiftBan = 0.5f;

        /// <summary>
        ///     Behavior when switching from neutral to forward or reverse gear.
        /// </summary>
        [FormerlySerializedAs("reverseType")]
        [Tooltip("    Behavior when switching from neutral to forward or reverse gear.")]
        public AutomaticTransmissionReverseType
            automaticTransmissionReverseType = AutomaticTransmissionReverseType.Auto;

        /// <summary>
        ///     If true revs will be matched when up/down shifting. Highly recommended, especially with automatic gearbox and
        ///     clutch.
        /// </summary>
        [Tooltip(
            "    If true revs will be matched when up/down shifting. Highly recommended, especially with automatic gearbox and\r\n    clutch.")]
        [ShowInSettings("Rev Match")]
        public bool revMatch = true;

        /// <summary>
        ///     Time during which all the shift checks have to be valid for shift to happen.
        /// </summary>
        [Tooltip("    Time during which all the shift checks have to be valid for shift to happen.")]
        public float shiftCheckCooldown = 0.1f;

        /// <summary>
        ///     List of shift checks that all have to return true before the vehicle can shift.
        /// </summary>
        [HideInInspector]
        [Tooltip("    List of shift checks that all have to return true before the vehicle can shift.")]
        public List<ShiftCheck> shiftChecks = new List<ShiftCheck>();

        /// <summary>
        ///     True when all shift conditions are fulfilled.
        /// </summary>
        [Tooltip("    True when all shift conditions are fulfilled.")]
        [ShowInTelemetry]
        public bool shiftCheckValid = true;

        /// <summary>
        ///     Function that changes the gears as required.
        ///     Use transmissionType External and assign this delegate to use your own gear shift code.
        /// </summary>
        [Tooltip(
            "Function that changes the gears as required.\r\nUse transmissionType External and assign this delegate to use your own gear shift code.")]
        public Shift shiftDelegate;

        /// <summary>
        ///     Time it takes transmission to shift between gears.
        /// </summary>
        [Tooltip("    Time it takes transmission to shift between gears.")]
        [ShowInSettings("Shift Duration", 0.001f, 0.5f, 0.05f)]
        public float shiftDuration = 0.2f;

        /// <summary>
        ///     Intensity of variable shift point. Higher value will result in shift point moving higher up with higher engine
        ///     load.
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "Intensity of variable shift point. Higher value will result in shift point moving higher up with higher engine load.")]
        public float variableShiftIntensity = 0.3f;

        /// <summary>
        ///     If enabled shifting when in manual transmission will be instant, ignoring post shift ban.
        /// </summary>
        [Tooltip("    If enabled shifting when in manual transmission will be instant, ignoring post shift ban.")]
        public bool ignorePostShiftBanInManual = true;

        /// <summary>
        ///     If enabled transmission will adjust both shift up and down points to match current load.
        /// </summary>
        [Tooltip("    If enabled transmission will adjust both shift up and down points to match current load.")]
        [ShowInSettings("Variable Shift Point")]
        public bool variableShiftPoint = true;

        /// <summary>
        /// Current gear ratio.
        /// </summary>
        [UnityEngine.Tooltip("Current gear ratio.")]
        [ShowInTelemetry]
        public float ratio;

        [SerializeField]
        [ShowInTelemetry]
        private int _currentGearIndex;

        /// <summary>
        ///     RPM at which automatic transmission will shift down. If dynamic shift point is enabled this value will change
        ///     depending on load.
        /// </summary>
        [Tooltip(
            "RPM at which automatic transmission will shift down. If dynamic shift point is enabled this value will change depending on load.")]
        [SerializeField]
        private float _downshiftRPM = 1400;

        /// <summary>
        ///     Engine RPM at which transmission will shift down if dynamic shift point is enabled.
        /// </summary>
        [SerializeField]
        [ShowInTelemetry]
        private float _targetDownshiftRPM;

        /// <summary>
        ///     Engine RPM at which transmission will shift up if dynamic shift point is enabled.
        /// </summary>
        [SerializeField]
        [ShowInTelemetry]
        private float _targetUpshiftRPM;

        /// <summary>
        ///     Determines in which way gears can be changed.
        ///     Manual - gears can only be shifted by manual user input.
        ///     Automatic - automatic gear changing. Allows for gear skipping (e.g. 3rd->5th) which can be useful in trucks and
        ///     other high gear count vehicles.
        ///     AutomaticSequential - automatic gear changing but only one gear at the time can be shifted (e.g. 3rd->4th)
        /// </summary>
        [SerializeField]
        [Tooltip("Manual - gears can only be shifted by manual user input. " +
                 "Automatic - automatic gear changing. Allows for gear skipping (e.g. 3rd->5th) which can be useful in trucks and other high gear count vehicles. " +
                 "AutomaticSequential - automatic gear changing but only one gear at the time can be shifted (e.g. 3rd->4th)")]
        [ShowInSettings("Type")]
        private Type _transmissionType = Type.Automatic;

        /// <summary>
        ///     RPM at which automatic transmission will shift up. If dynamic shift point is enabled this value will change
        ///     depending on load.
        /// </summary>
        [Tooltip(
            "RPM at which automatic transmission will shift up. If dynamic shift point is enabled this value will change depending on load.")]
        [SerializeField]
        [ShowInTelemetry]
        private float _upshiftRPM = 2800;

        private Shift _cAutoShift;
        private Shift _cManualShift;
        private Shift _cCVTShift;

        private float _cvtGearRatio;

        [ShowInSettings("Sequential")]
        public bool isSequential = false;

        public bool externalShiftChecksValid = true;

        /// <summary>
        ///     List of gears starting with reverse gears, neutral and then forward gears. List is constructed on initialization
        ///     from forward and reverse gear lists.
        ///     This is so that things like Gear++ and Gear-- can be done.
        /// </summary>
        private List<float> _gearRatios = new List<float>();

        private GearShift _lastGearShift;
        private float _lastShiftCheckTime = -999f;
        private float _smoothedYAxis01;
        private float _verticalInputChangeVelocity;
        private bool _repeatInputFlag;
        private int _prevForwardGearCount;
        private int _prevReverseGearCount;

        /// <summary>
        ///     Timer needed to prevent manual transmission from slipping out of gear too soon when hold in gear is enabled,
        ///     which could happen in FixedUpdate() runs twice for one Generate() and the shift flag is reset
        ///     resulting in gearbox thinking it has no shift input.
        /// </summary>
        private float _slipOutOfGearTimer = -999f;

        /// <summary>
        ///     Info about the last gear shift that happened.
        /// </summary>
        public GearShift LastGearShift
        {
            get { return _lastGearShift; }
            private set { _lastGearShift = value; }
        }

        /// <summary>
        ///     Angular velocity at which transmission will aim to downshift.
        /// </summary>
        public float DownshiftAngularVelocity
        {
            get { return UnitConverter.RPMToAngularVelocity(_downshiftRPM); }
        }

        /// <summary>
        ///     Current RPM at which transmission will aim to downshift. All the modifiers are taken into account.
        ///     This value changes with driving conditions.
        /// </summary>
        public float DownshiftRPM
        {
            get { return _downshiftRPM; }
            set { _downshiftRPM = Mathf.Clamp(value, 0, Mathf.Infinity); }
        }

        /// <summary>
        ///     Is the transmission in forward gear? Gear ratio > 0.
        /// </summary>
        public bool InForwardGear
        {
            get { return ratio > 0; }
        }

        /// <summary>
        ///     Is the transmission in neutral gear? Gear ratio == 0.
        /// </summary>
        public bool InNeutralGear
        {
            get { return ratio < 0.0001f && ratio > -0.0001f; }
        }

        /// <summary>
        ///     Is the transmission in reverse gear? Gear ratio < 0.
        /// </summary>
        public bool InReverseGear
        {
            get { return ratio < 0; }
        }

        /// <summary>
        ///     Number of forward gears.
        /// </summary>
        public int ForwardGearCount
        {
            get { return _gearRatios.Count - 1 - gearingProfile.reverseGears.Count; }
        }

        /// <summary>
        ///     List of forward gears. Gears list will be updated if new value is assigned.
        /// </summary>
        public List<float> ForwardGears
        {
            get { return gearingProfile.forwardGears; }
            set
            {
                gearingProfile.forwardGears = value;
                ReconstructGearList();
            }
        }

        /// <summary>
        ///     0 for neutral, less than 0 for reverse gears and lager than 0 for forward gears.
        ///     Use 'ShiftInto' to set gear.
        /// </summary>
        public int Gear
        {
            get
            {
                int reverseGearCount = gearingProfile.reverseGears.Count;
                int gearRatioCount = _gearRatios.Count;
                if (_currentGearIndex < 0 - reverseGearCount)
                {
                    return _currentGearIndex = 0 - reverseGearCount;
                }

                if (_currentGearIndex >= gearRatioCount - reverseGearCount - 1)
                {
                    return _currentGearIndex = gearRatioCount - reverseGearCount - 1;
                }

                return _currentGearIndex;
            }
        }

        /// <summary>
        ///     Returns current gear name as a string, e.g. "R", "R2", "N" or "1"
        /// </summary>

        public string GearName
        {
            get
            {
                float gear = Gear;
                if (gear == 0)
                {
                    return "N";
                }

                if (gear > 0)
                {
                    return Gear.ToString();
                }

                if (gearingProfile.reverseGears.Count > 1)
                {
                    return "R" + (gear < 0 ? -gear : gear);
                }

                return "R";
            }
        }

        /// <summary>
        ///     List of all gear ratios including reverse, forward and neutral gears. e.g. -2nd, -1st, 0 (netural), 1st, 2nd, 3rd,
        ///     etc.
        /// </summary>
        public List<float> Gears
        {
            get { return _gearRatios; }
        }

        public bool IsInReverse
        {
            get { return ratio < 0; }
        }

        /// <summary>
        ///     True if currently shifting.
        /// </summary>
        public bool IsShifting
        {
            get { return !LastGearShift.HasEnded; }
        }

        /// <summary>
        ///     Total current transmission ratio.
        /// </summary>

        public float Ratio
        {
            get { return ratio; }
        }

        /// <summary>
        ///     Number of reverse gears.
        /// </summary>
        public int ReverseGearCount
        {
            get { return gearingProfile.reverseGears.Count; }
        }

        /// <summary>
        ///     List of reverse gears. Gears list will be updated if new value is assigned.
        /// </summary>
        public List<float> ReverseGears
        {
            get { return gearingProfile.reverseGears; }
            set
            {
                gearingProfile.reverseGears = value;
                ReconstructGearList();
            }
        }

        /// <summary>
        ///     RPM at which the transmission will try to downshift, but the value might get changed by shift modifier such
        ///     as incline modifier.
        ///     To get actual downshift RPM use DownshiftRPM.
        /// </summary>

        public float TargetDownshiftRPM
        {
            get { return _targetDownshiftRPM; }
        }

        /// <summary>
        ///     RPM at which the transmission will try to upshift, but the value might get changed by shift modifier such
        ///     as incline modifier.
        ///     To get actual upshift RPM use UpshiftRPM.
        /// </summary>

        public float TargetUpshiftRPM
        {
            get { return _targetUpshiftRPM; }
        }

        /// <summary>
        ///     Type of transmission.
        /// </summary>
        public Type TransmissionType
        {
            get { return _transmissionType; }
            set { _transmissionType = value; }
        }

        /// <summary>
        ///     Angular velocity at which the transmission will try to upshift. For RPM use Upshift RPM.
        /// </summary>
        public float UpshiftAngularVelocity
        {
            get { return UnitConverter.RPMToAngularVelocity(_upshiftRPM); }
        }

        /// <summary>
        ///     Actual upshift RPM with all the modifiers taken into account.
        /// </summary>
        public float UpshiftRPM
        {
            get { return _upshiftRPM; }
            set { _upshiftRPM = Mathf.Clamp(value, 0, Mathf.Infinity); }
        }


        public override void Initialize(VehicleController vc)
        {
            base.Initialize(vc);
            _lastGearShift = new GearShift();

            ReconstructGearList();
            _cAutoShift = AutomaticShift;
            _cManualShift = ManualShift;
            _cCVTShift = CVTShift;

            _prevForwardGearCount = gearingProfile.forwardGears.Count;
            _prevReverseGearCount = gearingProfile.reverseGears.Count;

            if (_transmissionType == Type.AutomaticSequential_Obsolete)
            {
                _transmissionType = Type.Automatic;
            }
        }


        public override void OnPrePhysicsStep(float dt)
        {
            base.OnPrePhysicsStep(dt);

            int forwardGearCount = gearingProfile.forwardGears.Count;
            int reverseGearCount = gearingProfile.reverseGears.Count;

            if (forwardGearCount != _prevForwardGearCount || reverseGearCount != _prevReverseGearCount)
            {
                ReconstructGearList();
            }

            _prevForwardGearCount = forwardGearCount;
            _prevReverseGearCount = reverseGearCount;


            // Gear shifting
            ratio = GetCurrentGearRatio();

            bool currentShiftCheckValid = !(vc.HasWheelSpin || vc.HasWheelSkid || vc.HasWheelAir)
                && vc.powertrain.clutch.IsFullyEngaged
                && externalShiftChecksValid;

            if (!currentShiftCheckValid)
            {
                _lastShiftCheckTime = vc.realtimeSinceStartup;
                shiftCheckValid = false;
            }
            else if (_lastShiftCheckTime + shiftCheckCooldown <= vc.realtimeSinceStartup)
            {
                shiftCheckValid = true;
            }

            if (_transmissionType == Type.Manual)
            {
                shiftDelegate = _cManualShift;
            }
            else if (_transmissionType == Type.Automatic)
            {
                shiftDelegate = _cAutoShift;
            }
            else if (_transmissionType == Type.CVT)
            {
                shiftDelegate = _cCVTShift;
            }


            // Run shift checks
            externalShiftChecksValid = true;
            for (int i = 0; i < shiftChecks.Count; i++)
            {
                ShiftCheck check = shiftChecks[i];
                if (!check.Invoke())
                {
                    externalShiftChecksValid = false;
                    break;
                }
            }

            shiftDelegate.Invoke(vc, externalShiftChecksValid);

            vc.input.ResetShiftFlags();
        }



        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            inertia = 0.02f;
            gearingProfile =
                Resources.Load(VehicleController.defaultResourcesPath + "DefaultGearingProfile")
                    as TransmissionGearingProfile;

            cvtMaxInputTorque = vc.powertrain.engine.PeakTorque * 1.5f;
        }


        public override void Validate(VehicleController vc)
        {
            base.Validate(vc);

            Debug.Assert(!string.IsNullOrEmpty(outputASelector.name),
                         "Transmission is not connected to anything. Go to Powertrain > Transmission and set the output.");

            if (gearingProfile == null)
            {
                Debug.LogError("Transmission gearing profile not assigned.");
            }
            else
            {
                foreach (float gear in gearingProfile.reverseGears)
                {
                    Debug.Assert(gear < 0, "Reverse gears in gearing profile should have negative value.");
                }

                foreach (float gear in gearingProfile.forwardGears)
                {
                    Debug.Assert(gear > 0, "Forward gears in gearing profile should have positive value.");
                }
            }

            if (_upshiftRPM > vc.powertrain.engine.revLimiterRPM || _upshiftRPM > vc.powertrain.engine.maxRPM)
            {
                Debug.LogWarning(
                    "Transmission upshift RPM set to higher RPM than the engine can achieve (check engine maxRPM and revLimiterRPM).");
            }

            if (_downshiftRPM < vc.powertrain.engine.stallRPM || _downshiftRPM < vc.powertrain.engine.idleRPM)
            {
                Debug.LogWarning(
                    "Transmission downshift RPM set to lower RPM than the engine can achieve (check engine stallRPM and idleRPM).");
            }
        }


        /// <summary>
        ///     Total gear ratio of the transmission for current gear.
        /// </summary>
        public float GetCurrentGearRatio()
        {
            return _gearRatios[GearToIndex(_currentGearIndex)] * finalGearRatio;
        }


        /// <summary>
        ///     Total gear ratio of the transmission for the specific gear.
        /// </summary>
        /// <returns></returns>
        public float GetGearRatio(int g)
        {
            return _gearRatios[GearToIndex(g)] * finalGearRatio;
        }


        public override float QueryAngularVelocity(float inputAngularVelocity, float dt)
        {
            _angularVelocity = inputAngularVelocity;
            if (ratio == 0 || _outputAIsNull)
            {
                return inputAngularVelocity;
            }

            return outputA.QueryAngularVelocity(inputAngularVelocity / ratio, dt) * ratio;
        }


        public override float QueryInertia()
        {
            if (_outputAIsNull || ratio == 0)
            {
                return inertia;
            }

            return inertia + outputA.QueryInertia() / (ratio * ratio);
        }


        /// <summary>
        ///     Recreates gear list from the forward and reverse gears lists.
        /// </summary>
        public void ReconstructGearList()
        {
            if (gearingProfile == null)
            {
                Debug.LogError("GearingProfile is null. Gear list will not be generated.");
                return;
            }

            _gearRatios.Clear();
            _gearRatios.AddRange(gearingProfile.reverseGears);
            _gearRatios.Add(0);
            _gearRatios.AddRange(gearingProfile.forwardGears);
        }


        /// <summary>
        ///     Converts axle RPM to engine RPM for given gear in Gears list.
        /// </summary>
        public float ReverseTransmitRPM(float inputRPM, int g)
        {
            float outRpm = inputRPM * _gearRatios[GearToIndex(g)] * finalGearRatio;
            outRpm = outRpm < 0 ? -outRpm : outRpm;
            return outRpm;
        }



        public override float ForwardStep(float torque, float inertiaSum, float dt)
        {
            _torque = torque;
            if (_outputAIsNull)
            {
                return torque;
            }

            if (ratio == 0)
            {
                outputA.ForwardStep(0, inertiaSum, dt);
                return torque;
            }

            if (_transmissionType == Type.CVT)
            {
                float minRatio = gearingProfile.forwardGears[0];
                float maxRatio = minRatio * 25f;
                float torqueFactor = (torque < 0f ? 0 : torque) / cvtMaxInputTorque;
                torqueFactor = torqueFactor < 0f ? 0f : torqueFactor > 1f ? 1f : torqueFactor;

                float newCvtRatio = maxRatio * torqueFactor + minRatio * (1f - torqueFactor);
                _cvtGearRatio = Mathf.Lerp(_cvtGearRatio, newCvtRatio, dt * 10f);
                ratio = _cvtGearRatio * finalGearRatio * (ratio < 0 ? -1f : 1f);
            }

            // Always send torque to keep wheels updated
            return outputA.ForwardStep(torque * ratio, (inertiaSum + inertia) * (ratio * ratio), dt) / ratio;
        }



        /// <summary>
        ///     Shifts into given gear. 0 for neutral, less than 0 for reverse and above 0 for forward gears.
        ///     Does nothing if the target gear is equal to current gear.
        /// </summary>
        public void ShiftInto(int g, bool instant = false)
        {
            if (g == Gear || g < -100)
            {
                return;
            }

            if (gearingProfile == null)
            {
                Debug.LogError("GearingProfile is null. Can not change gears.");
                return;
            }

            int prevGear = Gear;

            bool postShiftBanActive = !instant && vc.realtimeSinceStartup < LastGearShift.EndTime + postShiftBan;
            if (instant || externalShiftChecksValid && !postShiftBanActive)
            {
                int reverseGearCount = gearingProfile.reverseGears.Count;
                int gearRatioCount = _gearRatios.Count;

                if (g < 0 - reverseGearCount)
                {
                    _currentGearIndex = 0 - reverseGearCount;
                }
                else if (g >= gearRatioCount - reverseGearCount - 1)
                {
                    _currentGearIndex = ForwardGearCount;
                }
                else
                {
                    _currentGearIndex = g;
                }

                float fromRPM = RPM;
                float toRPM = _currentGearIndex == 0 || prevGear == 0
                                  ? -1
                                  : fromRPM / GetGearRatio(prevGear) * GetGearRatio(_currentGearIndex);

                float thisShiftDuration = instant ? 0f : shiftDuration;
                _lastGearShift.RegisterShift(vc.realtimeSinceStartup, thisShiftDuration, _currentGearIndex, prevGear,
                                             fromRPM, toRPM);

                if (_lastGearShift.IsUpshift)
                {
                    onUpshift.Invoke(_lastGearShift);
                }
                else
                {
                    onDownshift.Invoke(_lastGearShift);
                }

                onShift.Invoke(_lastGearShift);

                if (g == 0)
                {
                    _repeatInputFlag = false;
                }
            }
        }


        private void CVTShift(VehicleController vc, bool shiftChecksValid)
        {
            AutomaticShift(vc, shiftChecksValid);
        }

        /// <summary>
        ///     Handles automatic and automatic sequential shifting.
        /// </summary>
        private void AutomaticShift(VehicleController vc, bool shiftChecksValid)
        {
            float vehicleSpeed = vc.Speed;
            float engineRPM = vc.powertrain.engine.RPM;
            float throttleInput = vc.input.InputSwappedThrottle;
            float brakeInput = vc.input.InputSwappedBrakes;
            float damping = throttleInput > _smoothedYAxis01 ? 0.2f : 2f;
            _smoothedYAxis01 =
                Mathf.SmoothDamp(_smoothedYAxis01, throttleInput, ref _verticalInputChangeVelocity, damping);

            _targetDownshiftRPM = _downshiftRPM;
            _targetUpshiftRPM = _upshiftRPM;

            if (variableShiftPoint)
            {
                float revLimiterRPM = vc.powertrain.engine.revLimiterRPM;
                float minEngineRPM = vc.powertrain.engine.minRPM;
                _targetUpshiftRPM =
                    _upshiftRPM + Mathf.Clamp01(_smoothedYAxis01 * variableShiftIntensity) * revLimiterRPM;
                _targetDownshiftRPM =
                    _downshiftRPM + Mathf.Clamp01(_smoothedYAxis01 * variableShiftIntensity) * revLimiterRPM;

                _targetUpshiftRPM = Mathf.Clamp(_targetUpshiftRPM, _upshiftRPM, revLimiterRPM * 0.98f);
                _targetDownshiftRPM = Mathf.Clamp(_targetDownshiftRPM, minEngineRPM * 1.1f, _targetUpshiftRPM * 0.6f);

                // Intentionally allow incline modifier to go over rev limiter RPM to prevent upshifts on inclines
                float inclineModifier =
                    Mathf.Clamp01(Vector3.Dot(vc.vehicleTransform.forward, Vector3.up) * inclineEffectCoeff);
                inclineModifier *= inclineModifier;
                _targetUpshiftRPM += revLimiterRPM * (inclineModifier * 3f);
                _targetDownshiftRPM += revLimiterRPM * (inclineModifier * 1.8f);
            }

            int gear = Gear;

            // In neutral
            if (gear == 0)
            {
                if (automaticTransmissionReverseType == AutomaticTransmissionReverseType.Auto)
                {
                    if (throttleInput > INPUT_DEADZONE)
                    {
                        ShiftInto(1, true);
                    }
                    else if (brakeInput > INPUT_DEADZONE)
                    {
                        ShiftInto(-1, true);
                    }
                }
                else if (automaticTransmissionReverseType == AutomaticTransmissionReverseType.RequireShiftInput)
                {
                    if (vc.input.ShiftUp || vc.input.ShiftInto == 1)
                    {
                        ShiftInto(1, true);
                    }
                    else if (vc.input.ShiftDown || vc.input.ShiftInto == -1)
                    {
                        ShiftInto(-1, true);
                    }
                }
                else if (automaticTransmissionReverseType == AutomaticTransmissionReverseType.RepeatInput)
                {
                    if (_repeatInputFlag == false && throttleInput < INPUT_DEADZONE && brakeInput < INPUT_DEADZONE)
                    {
                        _repeatInputFlag = true;
                    }

                    if (_repeatInputFlag)
                    {
                        if (throttleInput > INPUT_DEADZONE)
                        {
                            ShiftInto(1, true);
                        }
                        else if (brakeInput > INPUT_DEADZONE)
                        {
                            ShiftInto(-1, true);
                        }
                    }
                }
            }
            // In reverse
            else if (gear < 0)
            {
                // Shift into neutral
                if (automaticTransmissionReverseType != AutomaticTransmissionReverseType.RequireShiftInput)
                {
                    if (vehicleSpeed < 0.4f && (brakeInput > INPUT_DEADZONE || throttleInput < INPUT_DEADZONE))
                    {
                        ShiftInto(0);
                    }
                }
                else
                {
                    if (vc.input.ShiftUp || vc.input.ShiftInto == 0)
                    {
                        ShiftInto(0);
                    }
                    else if (vc.input.ShiftInto == 1)
                    {
                        ShiftInto(1, true);
                    }
                }

                // Reverse upshift
                float absGearMinusOne = gear - 1;
                absGearMinusOne = absGearMinusOne < 0 ? -absGearMinusOne : absGearMinusOne;
                if (engineRPM > TargetUpshiftRPM && absGearMinusOne < ReverseGearCount)
                {
                    ShiftInto(gear - 1);
                }
                // Reverse downshift
                else if (engineRPM < TargetDownshiftRPM && gear < -1)
                {
                    ShiftInto(gear + 1);
                }
            }
            // In forward
            else if (gear > 0)
            {
                if (vehicleSpeed > 0.4f)
                {
                    // Upshift
                    if (gear < ForwardGearCount && engineRPM > TargetUpshiftRPM && shiftCheckValid)
                    {
                        if (!isSequential)
                        {
                            int g = gear;

                            while (g < ForwardGearCount)
                            {
                                g++;

                                float wouldBeEngineRPM = ReverseTransmitRPM(RPM / ratio, g);
                                if (wouldBeEngineRPM < TargetDownshiftRPM)
                                {
                                    g--;
                                    break;
                                }
                            }

                            if (g != gear)
                            {
                                ShiftInto(g);
                            }
                        }
                        else
                        {
                            ShiftInto(gear + 1);
                        }
                    }
                    // Downshift
                    else if (engineRPM < TargetDownshiftRPM)
                    {
                        // Non-sequential
                        if (!isSequential)
                        {
                            if (gear != 1)
                            {
                                int g = gear;
                                while (g > 1)
                                {
                                    g--;
                                    float wouldBeEngineRPM = ReverseTransmitRPM(RPM / ratio, g);
                                    if (wouldBeEngineRPM > TargetUpshiftRPM)
                                    {
                                        g++;
                                        break;
                                    }
                                }

                                if (g != gear)
                                {
                                    ShiftInto(g);
                                }
                            }
                            else if (vehicleSpeed < 1f && throttleInput < INPUT_DEADZONE && brakeInput < INPUT_DEADZONE
                                     && automaticTransmissionReverseType != AutomaticTransmissionReverseType.RequireShiftInput)
                            {
                                ShiftInto(0);
                            }
                        }
                        // Sequential
                        else
                        {
                            if (Gear != 1)
                            {
                                ShiftInto(Gear - 1);
                            }
                            else if (vehicleSpeed < 1f && throttleInput < INPUT_DEADZONE && brakeInput < INPUT_DEADZONE
                                    && automaticTransmissionReverseType != AutomaticTransmissionReverseType.RequireShiftInput)
                            {
                                ShiftInto(0);
                            }
                        }
                    }
                }
                // Shift into neutral
                else
                {
                    if (automaticTransmissionReverseType != AutomaticTransmissionReverseType.RequireShiftInput)
                    {
                        if (vehicleSpeed < 0.4f && throttleInput < 0.2f)
                        {
                            ShiftInto(0);
                        }
                    }
                    else
                    {
                        if (vc.input.ShiftDown || vc.input.ShiftInto == 0)
                        {
                            ShiftInto(0, true);
                        }
                        else if (vc.input.ShiftInto == -1 && vc.Speed < 1f)
                        {
                            ShiftInto(-1, true);
                        }
                    }
                }
            }
        }

        private int GearToIndex(int g)
        {
            return g + gearingProfile.reverseGears.Count;
        }


        private void ManualShift(VehicleController vc, bool shiftChecksValid)
        {
            if (vc.input.ShiftUp)
            {
                ShiftInto(Gear + 1, ignorePostShiftBanInManual);
                return;
            }

            if (vc.input.ShiftDown)
            {
                ShiftInto(Gear - 1, ignorePostShiftBanInManual);
                return;
            }

            int shiftIntoSignal = vc.input.ShiftInto;
            if (shiftIntoSignal > -100)
            {
                ShiftInto(shiftIntoSignal, ignorePostShiftBanInManual);
                _slipOutOfGearTimer = 0;
            }
            else if (holdToKeepInGear)
            {
                _slipOutOfGearTimer += vc.fixedDeltaTime;
                if (Gear != 0 && _slipOutOfGearTimer > 0.1f)
                {
                    ShiftInto(0, ignorePostShiftBanInManual);
                }
            }
        }


        [Serializable]
        public partial class ShiftEvent : UnityEvent<GearShift>
        {
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Powertrain
{
    [CustomPropertyDrawer(typeof(TransmissionComponent))]
    public partial class TransmissionComponentDrawer : PowertrainComponentDrawer
    {
        private TransmissionComponent _transmissionComponent;


        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            _transmissionComponent =
                SerializedPropertyHelper.GetTargetObjectOfProperty(property) as TransmissionComponent;
            SerializedProperty transmissionType = property.FindPropertyRelative("_transmissionType");
            TransmissionComponent.Type type = (TransmissionComponent.Type)transmissionType.enumValueIndex;

            DrawCommonProperties();

            drawer.BeginSubsection("General");
            drawer.Field("_transmissionType");
            if (type == TransmissionComponent.Type.CVT)
            {
                drawer.Field("cvtMaxInputTorque", true, "Nm", "CVT Max Input Torque");
            }
            drawer.EndSubsection();


            // SHIFTING SETTIGNS
            if (type != TransmissionComponent.Type.CVT)
            {
                drawer.BeginSubsection("Shifting");
                drawer.Field("isSequential");
                drawer.Field("revMatch");
                drawer.Field("shiftDuration", true, "s");
                drawer.Field("postShiftBan", true, "s");

                if (type != TransmissionComponent.Type.Manual)
                {
                    drawer.Field("automaticTransmissionReverseType");
                    drawer.Field("_upshiftRPM", true, "rpm");
                    drawer.Field("_downshiftRPM", true, "rpm");
                    drawer.Field("_currentGearIndex");
                    if (drawer.Field("variableShiftPoint").boolValue)
                    {
                        drawer.Field("variableShiftIntensity");
                        drawer.Field("inclineEffectCoeff");
                        drawer.Info(
                            "High Incline Effect Coefficient values can prevent vehicle from changing gears as it is possible to get the Target Upshift RPM value higher than Rev Limiter RPM value. " +
                            "This is intentional to prevent heavy vehicles from upshifting on steep inclines.");
                        drawer.Field("_targetUpshiftRPM", false, "rpm");
                        drawer.Field("_targetDownshiftRPM", false, "rpm");
                    }

                    drawer.EndSubsection();
                }
                else
                {
                    drawer.Field("holdToKeepInGear");
                    drawer.Field("ignorePostShiftBanInManual");
                    drawer.EndSubsection();
                }
            }


            // SHIFTING SETTINGS
            if (type == TransmissionComponent.Type.Automatic)
            {
                drawer.BeginSubsection("Shift Conditions");
                drawer.Field("shiftCheckCooldown");
                drawer.Space(5);
                if (Application.isPlaying)
                {
                    drawer.Label("Wheel Spin: ", _transmissionComponent.vc.HasWheelSpin);
                    drawer.Label("Wheel Skid: ", _transmissionComponent.vc.HasWheelSkid);
                    drawer.Label("Wheel Air: ", _transmissionComponent.vc.HasWheelAir);
                    drawer.Label("Clutch Engaged: ", _transmissionComponent.vc.powertrain.clutch.IsFullyEngaged);
                    drawer.Label("External Checks", _transmissionComponent.externalShiftChecksValid);
                }
                else
                {
                    drawer.Info("Checks are visible doing play mode.");
                }
                drawer.EndSubsection();
            }


            drawer.BeginSubsection("Gearing");
            if (Application.isPlaying)
            {
                drawer.Label($"Current Total Gear Ratio:\t{_transmissionComponent.Ratio}");
            }
            drawer.Field("finalGearRatio");
            drawer.Field("gearingProfile");
            drawer.EmbeddedObjectEditor<NVP_NUIEditor>(_transmissionComponent.gearingProfile, drawer.positionRect);
            drawer.EndSubsection();


            // TOP SPEEDS PER GEAR
            if (_transmissionComponent.gearingProfile != null)
            {
                VehicleController vc = property.serializedObject.targetObject as VehicleController;

                if (vc != null)
                {
                    WheelUAPI wc = vc.gameObject.GetComponentInChildren<WheelUAPI>();

                    if (wc != null)
                    {
                        drawer.BeginSubsection("Top Speed Per Gear");

                        float revLimiterRPM = vc.powertrain.engine.revLimiterRPM;
                        float wheelRadius = wc.Radius;

                        for (int i = 0; i < _transmissionComponent.gearingProfile.reverseGears.Count; i++)
                        {
                            float gearRatio = _transmissionComponent.gearingProfile.reverseGears[i];
                            float wheelRPM = revLimiterRPM / (gearRatio * _transmissionComponent.finalGearRatio);
                            float topSpeed = wheelRadius * (2f * Mathf.PI / 60f) * wheelRPM;
                            drawer.Label($"R{_transmissionComponent.gearingProfile.reverseGears.Count - i}:" +
                                $"\t{topSpeed.ToString("0.0")} m/s [" +
                                $"{(topSpeed * 3.6f).ToString("0.0")} km/h" +
                                $" | {(topSpeed * 2.24f).ToString("0.0")} mph]");
                        }

                        drawer.Label($"N:\t  0 ------------");

                        for (int i = 0; i < _transmissionComponent.gearingProfile.forwardGears.Count; i++)
                        {
                            float gearRatio = _transmissionComponent.gearingProfile.forwardGears[i];
                            float wheelRPM = revLimiterRPM / (gearRatio * _transmissionComponent.finalGearRatio);
                            float topSpeed = wheelRadius * (2f * Mathf.PI / 60f) * wheelRPM;
                            drawer.Label($"D{i + 1}:" +
                                $"\t  {topSpeed.ToString("0.0")} m/s [" +
                                $"{(topSpeed * 3.6f).ToString("0.0")} km/h" +
                                $" | {(topSpeed * 2.24f).ToString("0.0")} mph]");
                        }

                        drawer.EndSubsection();
                    }
                }
            }


            drawer.BeginSubsection("Events");
            drawer.Space(2);
            drawer.Field("onShift");
            drawer.Field("onUpshift");
            drawer.Field("onDownshift");
            drawer.EndSubsection();

            EditorGUI.EndDisabledGroup();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
