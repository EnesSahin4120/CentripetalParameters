using NWH.Common.SceneManagement;
using NWH.VehiclePhysics2.Modules.ABS;
using NWH.VehiclePhysics2.Modules.TCS;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.VehicleGUI
{
    /// <summary>
    ///     DashGUIController is a vehicle dashboard GUI or game GUI controller.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public partial class DashGUIController : MonoBehaviour
    {
        public enum DataSource
        {
            VehicleController,
            VehicleChanger,
        }

        [FormerlySerializedAs("ABS")]
        public DashLight absDashLight;

        public AnalogGauge analogRpmGauge;
        public AnalogGauge analogSpeedGauge;

        [FormerlySerializedAs("checkEngine")]
        public DashLight checkEngineDashLight;

        public DataSource dataSource;
        public DigitalGauge digitalGearGauge;
        public DigitalGauge digitalRpmGauge;
        public DigitalGauge digitalSpeedGauge;

        [FormerlySerializedAs("highBeam")]
        public DashLight highBeamDashLight;

        [FormerlySerializedAs("leftBlinker")]
        public DashLight leftBlinkerDashLight;

        [FormerlySerializedAs("lowBeam")]
        public DashLight lowBeamDashLight;

        [FormerlySerializedAs("rightBlinker")]
        public DashLight rightBlinkerDashLight;

        [FormerlySerializedAs("TCS")]
        public DashLight tcsDashLight;

        public bool useAbsDashLight;

        public bool useAnalogRpmGauge;
        public bool useAnalogSpeedGauge;
        public bool useCheckEngineDashLight;
        public bool useDigitalGearGauge;
        public bool useDigitalRpmGauge;
        public bool useDigitalSpeedGauge;
        public bool useHighBeamDashLight;
        public bool useLeftBlinkerDashLight;
        public bool useLowBeamDashLight;
        public bool useRightBlinkerDashLight;
        public bool useTcsDashLight;
        public VehicleController vehicleController;
        private Canvas _canvas;
        private VehicleController _prevVc;

        private ABSModule _absModule;
        private TCSModule _tcsModule;
        private bool _update = true;


        private void Awake()
        {
            _canvas = GetComponent<Canvas>();

            if (vehicleController != null)
            {
                vehicleController.onWake.AddListener(OnWake);
                vehicleController.onSleep.AddListener(OnSleep);
            }
        }


        private void Update()
        {
            if (dataSource == DataSource.VehicleChanger)
                vehicleController = VehicleChanger.ActiveVehicle as VehicleController;

            if (_prevVc != vehicleController && vehicleController != null)
            {
                _tcsModule = vehicleController.GetComponent<TCSModuleWrapper>()?.module;
                _absModule = vehicleController.GetComponent<ABSModuleWrapper>()?.module;
            }

            if (vehicleController != null && _update)
            {
                if (useAnalogRpmGauge) analogRpmGauge.Value = vehicleController.powertrain.engine.RPM;

                if (useDigitalRpmGauge) digitalRpmGauge.numericalValue = vehicleController.powertrain.engine.RPM;

                if (useAnalogSpeedGauge) analogSpeedGauge.Value = vehicleController.Speed * 3.6f;

                if (useDigitalSpeedGauge) digitalSpeedGauge.numericalValue = vehicleController.Speed * 3.6f;

                if (useDigitalGearGauge)
                    digitalGearGauge.stringValue = vehicleController.powertrain.transmission.GearName;

                if (useLeftBlinkerDashLight)
                    leftBlinkerDashLight.Active = vehicleController.effectsManager.lightsManager.leftBlinkers.On;

                if (useRightBlinkerDashLight)
                    rightBlinkerDashLight.Active = vehicleController.effectsManager.lightsManager.rightBlinkers.On;

                if (useLowBeamDashLight)
                    lowBeamDashLight.Active = vehicleController.effectsManager.lightsManager.lowBeamLights.On;

                if (useHighBeamDashLight)
                    highBeamDashLight.Active = vehicleController.effectsManager.lightsManager.highBeamLights.On;

                if (useTcsDashLight)
                    if (_tcsModule != null)
                        tcsDashLight.Active = _tcsModule.active;

                if (useAbsDashLight)
                    if (_absModule != null)
                        absDashLight.Active = _absModule.active;


                if (useCheckEngineDashLight)
                    checkEngineDashLight.Active = vehicleController.damageHandler.Damage > 0.9999f;
            }
            else
            {
                if (useAnalogRpmGauge) analogRpmGauge.Value = 0;

                if (useAnalogSpeedGauge) analogSpeedGauge.Value = 0;

                if (useDigitalSpeedGauge) digitalSpeedGauge.numericalValue = 0;

                if (useDigitalGearGauge) digitalGearGauge.stringValue = "";

                if (useLeftBlinkerDashLight) leftBlinkerDashLight.Active = false;

                if (useRightBlinkerDashLight) rightBlinkerDashLight.Active = false;

                if (useLowBeamDashLight) lowBeamDashLight.Active = false;

                if (useHighBeamDashLight) highBeamDashLight.Active = false;

                if (useTcsDashLight) tcsDashLight.Active = false;

                if (useAbsDashLight) absDashLight.Active = false;

                if (useCheckEngineDashLight) checkEngineDashLight.Active = false;
            }

            _prevVc = vehicleController;
        }


        private void OnWake()
        {
            _canvas.enabled = true;
            _update = true;
        }


        private void OnSleep()
        {
            _canvas.enabled = false;
            _update = false;
        }
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.VehicleGUI
{
    [CustomEditor(typeof(DashGUIController))]
    [CanEditMultipleObjects]
    public partial class DashGUIControllerEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            if (drawer.Field("dataSource").enumValueIndex == 0)
            {
                drawer.Field("vehicleController");
            }
            else
            {
                drawer.Info(
                    "VehicleChanger is being used to get the active vehicle. Make sure you have one VehicleChanger present in your scene.");
            }

            drawer.BeginSubsection("Gauges");
            if (drawer.Field("useAnalogRpmGauge").boolValue)
            {
                drawer.Field("analogRpmGauge");
            }

            if (drawer.Field("useDigitalRpmGauge").boolValue)
            {
                drawer.Field("digitalRpmGauge");
            }

            if (drawer.Field("useAnalogSpeedGauge").boolValue)
            {
                drawer.Field("analogSpeedGauge");
            }

            if (drawer.Field("useDigitalSpeedGauge").boolValue)
            {
                drawer.Field("digitalSpeedGauge");
            }

            if (drawer.Field("useDigitalGearGauge").boolValue)
            {
                drawer.Field("digitalGearGauge");
            }

            drawer.EndSubsection();

            drawer.BeginSubsection("Dash Lights");
            if (drawer.Field("useLeftBlinkerDashLight").boolValue)
            {
                drawer.Field("leftBlinkerDashLight");
            }

            if (drawer.Field("useRightBlinkerDashLight").boolValue)
            {
                drawer.Field("rightBlinkerDashLight");
            }

            if (drawer.Field("useLowBeamDashLight").boolValue)
            {
                drawer.Field("lowBeamDashLight");
            }

            if (drawer.Field("useHighBeamDashLight").boolValue)
            {
                drawer.Field("highBeamDashLight");
            }

            if (drawer.Field("useTcsDashLight").boolValue)
            {
                drawer.Field("tcsDashLight");
            }

            if (drawer.Field("useAbsDashLight").boolValue)
            {
                drawer.Field("absDashLight");
            }

            if (drawer.Field("useCheckEngineDashLight").boolValue)
            {
                drawer.Field("checkEngineDashLight");
            }

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
