using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.Tests
{
    public partial class VehicleGeneralTests : MonoBehaviour
    {
        public VehicleController vc;


        public void Run()
        {
            vc = GetComponent<VehicleController>();

            EnableDisableTest();
            ComponentStateTest();
        }


        public void EnableDisableTest()
        {
            GameObject vehicleGO = vc.gameObject;
            vehicleGO.SetActive(false);
            vehicleGO.SetActive(true);
            vehicleGO.SetActive(false);
            vehicleGO.SetActive(true);
        }


        public void ComponentStateTest()
        {
            foreach (VehicleComponent component in vc.Components)
                if (component.IsOn)
                {
                    component.Enable();
                    Debug.Assert(component.IsEnabled, $"Component {component.GetType()} failed to enable.");

                    component.Enable();
                    Debug.Assert(component.IsEnabled, $"Component {component.GetType()} failed to enable.");

                    component.Disable();
                    Debug.Assert(!component.IsEnabled, $"Component {component.GetType()} failed to disable.");

                    component.Disable();
                    Debug.Assert(!component.IsEnabled, $"Component {component.GetType()} failed to disable.");

                    component.Enable();
                    Debug.Assert(component.IsEnabled, $"Component {component.GetType()} failed to enable.");
                }
        }
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Tests
{
    [CustomEditor(typeof(VehicleGeneralTests))]
    [CanEditMultipleObjects]
    public partial class VehicleGeneralTestsEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            VehicleGeneralTests test = (VehicleGeneralTests)target;

            if (drawer.Button("Run"))
            {
                test.Run();
            }

            drawer.EndEditor(this);
            return true;
        }
    }
}

#endif
