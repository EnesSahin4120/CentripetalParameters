using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.VehicleGUI
{
    public partial class AnalogGauge : MonoBehaviour
    {
        /// <summary>
        ///     angle of the needle at the highest value. You can use lock at end option to adjust this value while in play mode.
        /// </summary>
        [Tooltip(
            "angle of the needle at the highest value. You can use lock at end option to adjust this value while in play mode.")]
        public float endAngle = 330;

        /// <summary>
        ///     Locks the needle position at the end angle (play mode only).
        /// </summary>
        [Tooltip("    Locks the needle position at the end angle (play mode only).")]
        public bool lockAtEnd;

        /// <summary>
        ///     Locks the needle position at the start angle (play mode only).
        /// </summary>
        [Tooltip("    Locks the needle position at the start angle (play mode only).")]
        public bool lockAtStart;

        /// <summary>
        ///     Value at the end of needle travel, at the end angle.
        /// </summary>
        [Tooltip("    Value at the end of needle travel, at the end angle.")]
        public float maxValue;

        /// <summary>
        ///     Smooths the travel of the needle making it more inert, as if actually had some mass and resistance.
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "    Smooths the travel of the needle making it more inert, as if actually had some mass and resistance.")]
        public float needleSmoothing;

        /// <summary>
        ///     angle of the needle at the lowest value. You can use lock at start option to adjust this value while in play mode.
        /// </summary>
        [Tooltip(
            "angle of the needle at the lowest value. You can use lock at start option to adjust this value while in play mode.")]
        public float startAngle = 574;

        private float _angle;
        private float _currentValue;
        private GameObject _needle;
        private float _percent;
        private float _prevAngle;

        public float Value
        {
            get { return _currentValue; }
            set { _currentValue = Mathf.Clamp(value, 0, maxValue); }
        }


        private void Awake()
        {
            _needle = transform.Find("Needle").gameObject;
        }


        private void Start()
        {
            _angle = startAngle;
        }


        private void Update()
        {
            _percent = Mathf.Clamp01(_currentValue / maxValue);
            _prevAngle = _angle;
            _angle = Mathf.Lerp(startAngle + (endAngle - startAngle) * _percent, _prevAngle, needleSmoothing);

            if (lockAtEnd)
            {
                _angle = endAngle;
            }

            if (lockAtStart)
            {
                _angle = startAngle;
            }

            Transform t = _needle.transform;
            Vector3 currentAngle = t.localEulerAngles;
            t.localEulerAngles = new Vector3(currentAngle.x, currentAngle.y, _angle);
        }
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.VehicleGUI
{
    [CustomEditor(typeof(AnalogGauge))]
    [CanEditMultipleObjects]
    public partial class AnalogGaugeEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            drawer.Field("maxValue");
            drawer.Field("startAngle");
            drawer.Field("endAngle");
            drawer.Field("needleSmoothing");
            drawer.Field("lockAtStart");
            drawer.Field("lockAtEnd");
            drawer.Info("LockAtStart and LockAtEnd only work in play mode.");

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
