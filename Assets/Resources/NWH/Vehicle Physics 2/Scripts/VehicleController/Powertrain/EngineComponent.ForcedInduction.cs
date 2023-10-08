using System;
using UnityEngine;
using UnityEngine.Events;
using NWH.Common.Vehicles;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Powertrain
{
    public partial class EngineComponent
    {
        /// <summary>
        ///     Supercharger, turbocharger, etc.
        ///     Only an approximation. Engine capacity, air flow, turbo size, etc. are not taken into consideration.
        /// </summary>
        [Serializable]
        public partial class ForcedInduction
        {
            public enum ForcedInductionType
            {
                Turbocharger,
                Supercharger,
            }

            /// <summary>
            ///     Boost value as percentage in 0 to 1 range. Unitless.
            ///     Can be used for boost gauges.
            /// </summary>
            [Range(0, 1)]
            [ShowInTelemetry]
            [Tooltip("    Boost value as percentage in 0 to 1 range. Unitless.\r\n    Can be used for boost gauges.")]
            public float boost;

            /// <summary>
            ///     Type of forced induction.
            /// </summary>
            [Tooltip("    Type of forced induction.")]
            public ForcedInductionType forcedInductionType = ForcedInductionType.Turbocharger;

            /// <summary>
            ///     Imitates wastegate in a turbo setup.
            ///     Enable if you want turbo flutter sound effects and/or boost to drop off faster after closing throttle.
            ///     Not used with superchargers.
            /// </summary>
            [Tooltip(
                "Imitates wastegate in a turbo setup.\r\nEnable if you want turbo flutter sound effects and/or boost to drop off faster after closing throttle.\r\nNot used with superchargers.")]
            [ShowInSettings("Has Wastegate")]
            public bool hasWastegate = true;

            /// <summary>
            ///     Power coefficient that the maxPower of the engine will be multiplied by and represents power gained by
            ///     adding forced induction to the engine. E.g. 1.4 would mean that the engine will produce 140% of the maxPower.
            ///     Power gain is dependent on boost value.
            /// </summary>
            [Range(0, 2)]
            [Tooltip(
                "Additional power that will be added to the engine's power.\r\nThis is the maximum value possible and depends on spool percent.")]
            [ShowInSettings("Power Gain Mp.", 1f, 2f, 0.1f)]
            public float powerGainMultiplier = 1.4f;

            /// <summary>
            ///     Shortest time possible needed for turbo to spool up to its maximum RPM.
            ///     Use larger values for larger turbos and vice versa.
            ///     Forced to 0 for superchargers.
            /// </summary>
            [Range(0.1f, 4)]
            [Tooltip(
                "Shortest time possible needed for turbo to spool up to its maximum RPM.\r\nUse larger values for larger turbos and vice versa.\r\nForced to 0 for superchargers.")]
            [ShowInSettings("Spool Up Time", 0f, 2f, 0.05f)]
            public float spoolUpTime = 0.12f;

            /// <summary>
            ///     Should forced induction be used?
            /// </summary>
            [ShowInTelemetry]
            [Tooltip("    Should forced induction be used?")]
            [ShowInSettings("Enabled")]
            public bool useForcedInduction = true;

            /// <summary>
            ///     Cached boost value at the time of wastegate releasing pressure for sound effects.
            /// </summary>
            [Tooltip("    Cached boost value at the time of wastegate releasing pressure for sound effects.")]
            public float wastegateBoost;

            /// <summary>
            ///     Flag for sound effects.
            /// </summary>
            [Tooltip("    Flag for sound effects.")]
            public UnityEvent onWastegateRelease = new UnityEvent();


            private float _maxRpm = 120000;
            private float _rpm;
            private float _spoolVelocity;
            private float _boostVelocity;
            private float _pressure;

            /// <summary>
            ///     Current power gained from forced induction.
            /// </summary>
            public float PowerGainMultiplier
            {
                get
                {
                    if (!useForcedInduction)
                    {
                        return 1f;
                    }

                    float multiplier = Mathf.Lerp(1f, powerGainMultiplier, _rpm / _maxRpm);
                    return multiplier;
                }
            }


            public void Update(EngineComponent engine)
            {
                if (!useForcedInduction)
                {
                    return;
                }

                float targetRPM = _maxRpm * Mathf.Clamp01((engine.RPMPercent + 0.5f) * (engine.RPMPercent + 0.5f)) * engine._throttlePosition;

                if (forcedInductionType == ForcedInductionType.Turbocharger)
                {
                    _rpm = Mathf.SmoothDamp(_rpm, targetRPM, ref _spoolVelocity, targetRPM > _rpm ? spoolUpTime : spoolUpTime * 5f);

                    if (hasWastegate && engine._throttlePosition < 0.5f && boost > 0.5f)
                    {
                        onWastegateRelease.Invoke();
                        wastegateBoost = boost;
                        boost = 0f;
                    }
                }
                else
                {
                    _rpm = targetRPM;
                }


                _rpm = _rpm > _maxRpm ? _maxRpm : _rpm < 0 ? 0 : _rpm;

                float targetBoost = engine._throttlePosition * (_rpm / _maxRpm);
                boost = Mathf.SmoothDamp(boost, targetBoost, ref _boostVelocity, spoolUpTime * 0.5f);
            }
        }
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Powertrain
{
    [CustomPropertyDrawer(typeof(EngineComponent.ForcedInduction))]
    public partial class ForcedInductionDrawer : PowertrainComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("useForcedInduction");
            drawer.Field("forcedInductionType");
            drawer.Field("powerGainMultiplier");
            drawer.Field("spoolUpTime", true, "s");
            drawer.Field("hasWastegate");
            drawer.Field("boost", false);

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
