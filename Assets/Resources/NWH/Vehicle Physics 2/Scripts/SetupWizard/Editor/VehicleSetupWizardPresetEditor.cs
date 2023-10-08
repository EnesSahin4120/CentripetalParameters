#if UNITY_EDITOR
using NWH.NUI;
using UnityEditor;

namespace NWH.VehiclePhysics2.SetupWizard
{
    [CustomEditor(typeof(VehicleSetupWizardPreset))]
    [CanEditMultipleObjects]
    public partial class VehicleSetupWizardPresetEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            drawer.BeginSubsection("General");
            drawer.Field("vehicleType");
            drawer.EndSubsection();
            
            drawer.BeginSubsection("Physical Properties");
            drawer.Field("mass", true, "kg");
            drawer.Field("width", true, "m");
            drawer.Field("length", true, "m");
            drawer.Field("height", true, "m");
            drawer.EndSubsection();
            
            drawer.BeginSubsection("Powertrain");
            drawer.Field("enginePower", true, "kW");
            drawer.Field("engineMaxRPM");
            drawer.FloatSlider("transmissionGearing", 0.5f, 1.5f, "Long", "Short", true);
            drawer.Field("drivetrainConfiguration");
            drawer.EndSubsection();
            
            drawer.BeginSubsection("Suspension");
            drawer.FloatSlider("suspensionTravelCoeff", 0.5f, 2f, "Short", "Long", true);
            drawer.FloatSlider("suspensionStiffnessCoeff", 0.6f, 1.4f, "Soft", "Stiff", true);
            drawer.EndSubsection();

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
