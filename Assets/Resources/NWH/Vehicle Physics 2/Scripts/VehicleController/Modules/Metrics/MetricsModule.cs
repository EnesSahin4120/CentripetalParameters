using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.Metrics
{
    /// <summary>
    ///     Class for holding metrics such as odometer, top speed and drift time.
    /// </summary>
    [Serializable]
    public partial class MetricsModule : VehicleModule
    {
        public Metric averageSpeed = new Metric();
        public Metric continousDriftDistance = new Metric();
        public Metric continousDriftTime = new Metric();
        public Metric odometer = new Metric();
        public Metric topSpeed = new Metric();
        public Metric totalDriftDistance = new Metric();
        public Metric totalDriftTime = new Metric();

        private float _driftEndTime;
        private float _driftTimeout = 0.75f;


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            // Odometer
            odometer.Update(delegate { return vc.Speed * vc.realtimeSinceStartup; }, true);

            // Top speed
            topSpeed.Update(
                delegate
                {
                    if (vc.Speed > topSpeed.value)
                    {
                        return vc.Speed;
                    }

                    return topSpeed.value;
                }, false);

            // Average speed
            averageSpeed.Update(
                delegate { return odometer.value / vc.realtimeSinceStartup; }, false);

            // Total drift time
            totalDriftTime.Update(
                delegate
                {
                    if (vc.HasWheelSkid)
                    {
                        return vc.fixedDeltaTime;
                    }

                    return 0;
                }, true);

            // Continous drift time
            continousDriftTime.Update(
                delegate
                {
                    if (vc.HasWheelSkid)
                    {
                        _driftEndTime = vc.realtimeSinceStartup;
                        return vc.fixedDeltaTime;
                    }

                    if (vc.realtimeSinceStartup < _driftEndTime + _driftTimeout)
                    {
                        return vc.fixedDeltaTime;
                    }

                    return -continousDriftTime.value;
                }, true);

            // Total drift distance
            totalDriftDistance.Update(
                delegate
                {
                    if (vc.HasWheelSkid)
                    {
                        return vc.fixedDeltaTime * vc.Speed;
                    }

                    return 0;
                }, true);

            // Continous drift distance
            continousDriftDistance.Update(
                delegate
                {
                    if (vc.HasWheelSkid)
                    {
                        _driftEndTime = vc.realtimeSinceStartup;
                        return vc.fixedDeltaTime * vc.Speed;
                    }

                    if (vc.realtimeSinceStartup < _driftEndTime + _driftTimeout)
                    {
                        return vc.fixedDeltaTime * vc.Speed;
                    }

                    return -continousDriftDistance.value;
                }, true);
        }



        [Serializable]
        public partial class Metric
        {
            public delegate float UpdateDelegate();

            public float value;


            public void Update(UpdateDelegate del, bool increment)
            {
                if (increment)
                {
                    value += del();
                }
                else
                {
                    value = del();
                }
            }


            public void Reset()
            {
                value = 0;
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.Metrics
{
    [CustomPropertyDrawer(typeof(MetricsModule))]
    public partial class MetricsModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            MetricsModule metrics = SerializedPropertyHelper.GetTargetObjectOfProperty(property) as MetricsModule;
            if (metrics == null)
            {
                drawer.EndProperty();
                return false;
            }

            drawer.Label($"Top Speed: {metrics.topSpeed.value}");
            drawer.Label($"Average Speed: {metrics.averageSpeed.value}");
            drawer.Label($"Odometer: {metrics.odometer.value}");
            drawer.Label($"Cont. Drift Distance: {metrics.continousDriftDistance.value}");
            drawer.Label($"Cont. Drift Time: {metrics.continousDriftTime.value}");
            drawer.Label($"Total Drift Distance: {metrics.totalDriftDistance.value}");
            drawer.Label($"Total Drift Time: {metrics.totalDriftTime.value}");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
