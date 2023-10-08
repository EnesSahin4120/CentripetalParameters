#if UNITY_EDITOR
using NWH.NUI;
using UnityEditor;
using UnityEngine;

namespace NWH.VehiclePhysics2.SetupWizard
{
    [CustomEditor(typeof(VehicleSetupWizard))]
    [CanEditMultipleObjects]
    public partial class VehicleSetupWizardEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            VehicleSetupWizard sw = drawer.GetObject<VehicleSetupWizard>();
            if (sw == null)
            {
                return false;
            }

            if (PrefabUtility.GetPrefabInstanceStatus(sw.gameObject) == PrefabInstanceStatus.Connected)
            {
                drawer.Info("Setup Wizard should not be run in prefab mode, only scene mode.", MessageType.Warning);
            }

            if (sw.gameObject.GetComponentInChildren<Collider>() == null)
            {
                drawer.Info("Vehicle has no Colliders attached. Make sure to have at least one collider (BoxCollider, SphereCollider, etc.) present" +
                    " on the body of the vehicle!", MessageType.Error);
            }
            
            
            drawer.Space();

            drawer.BeginSubsection("Preset");
            drawer.Field("preset");
            drawer.EndSubsection();

            if (sw.preset == null)
            {
                drawer.EndEditor(this);
                return false;
            }
        
            drawer.BeginSubsection("Wheels");

            drawer.Field("wheelControllerType");

            if (sw.preset.vehicleType == VehicleSetupWizardPreset.VehicleType.Motorcycle)
            {
                drawer.Info(
                    "* Wheel GameObjects should be added in order: front, back (2 wheels total).\n" +
                    "* These objects should represent wheel models.\n" +
                    "* Make sure that no WheelController has been attached to the vehicle previously.\n");
            }
            else
            {
                drawer.Info(
                    "* Wheel GameObjects should be added in the left-right, front-to-back order e.g. frontLeft, frontRight, rearLeft, rearRight.\n" +
                    "* These objects should represent wheel models.\n" +
                    "* Make sure that no WheelController has been attached to the vehicle previously.\n");
            }

            drawer.ReorderableList("wheelGameObjects");
            drawer.EndSubsection();

            drawer.BeginSubsection("Options");
            drawer.Field("addCamera");
            drawer.Field("addCharacterEnterExitPoints");
            drawer.Field("registerWithVehicleChanger");
            drawer.Field("removeWizardOnComplete");

            drawer.EndSubsection();

            drawer.HorizontalRuler();

            VehicleController generatedVehicleController = null;
            if (drawer.Button("Run Setup"))
            {
                generatedVehicleController = VehicleSetupWizard.RunSetup(sw.gameObject, sw.wheelGameObjects, sw.wheelControllerType,
                                                                         sw.addCamera, sw.addCharacterEnterExitPoints, sw.registerWithVehicleChanger);
                if (generatedVehicleController != null && sw.preset != null)
                {
                    VehicleSetupWizard.RunConfiguration(generatedVehicleController, sw.preset);
                }
                else
                {
                    Debug.LogWarning("VehicleController or VehicleSetupWizardPreset are null, skipping post-setup configuration.");
                }
            }

            drawer.Info(
                "InputProvider will not be added automatically to the scene. Add VehicleInputProvider and SceneInputProvider" +
                "for used input type if they are already not present (e.g. InputSystemVehicleInputProvider and InputSystemSceneInputProvider. " +
                "Without this vehicle will not receive input.", MessageType.Warning);

            drawer.EndEditor(this);
            if (generatedVehicleController != null && sw.removeWizardOnComplete)
            {
                DestroyImmediate(sw);
            }

            return true;
        }


        public override bool UseDefaultMargins()
        {
            return false;
        }
    }
}

#endif
