using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Utility
{
    /// <summary>
    ///     Enables the object when vehicle is awake, disables it when vehicle is sleeping.
    /// </summary>
    public partial class FollowVehicleState : MonoBehaviour
    {
        private VehicleController _vc;


        private void Awake()
        {
            _vc = GetComponentInParent<VehicleController>();
            if (_vc == null)
            {
                Debug.LogError("VehicleController not found.");
            }

            _vc.onWake.AddListener(OnVehicleWake);
            _vc.onSleep.AddListener(OnVehicleSleep);

            if (_vc.IsAwake)
            {
                OnVehicleWake();
            }
            else
            {
                OnVehicleSleep();
            }
        }


        private void OnVehicleWake()
        {
            gameObject.SetActive(true);
        }


        private void OnVehicleSleep()
        {
            gameObject.SetActive(false);
        }
    }
}

#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Utility
{
    [CustomEditor(typeof(FollowVehicleState))]
    [CanEditMultipleObjects]
    public partial class FollowVehicleStateEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            drawer.Info("Enables/disables the GameObject based on Vehicle state (awake/asleep).");

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
