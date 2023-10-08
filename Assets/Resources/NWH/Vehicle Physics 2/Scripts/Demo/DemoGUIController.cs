using NWH.Common.Input;
using NWH.Common.SceneManagement;
using NWH.VehiclePhysics2.Modules.ABS;
using NWH.VehiclePhysics2.Modules.Aerodynamics;
using NWH.VehiclePhysics2.Modules.ESC;
using NWH.VehiclePhysics2.Modules.FlipOver;
using NWH.VehiclePhysics2.Modules.TCS;
using NWH.VehiclePhysics2.Modules.Trailer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NWH.VehiclePhysics2.Demo
{
    /// <summary>
    ///     Written only for demo purposes.
    ///     Messy code ahead - you have been warned!
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class DemoGUIController : MonoBehaviour
    {
        public static Color disabledColor = new Color32(66, 66, 66, 255);
        public static Color enabledColor = new Color32(76, 175, 80, 255);

        public Text promptText;

        public GameObject helpWindow;
        public GameObject settingsWindow;
        public GameObject telemetryWindow;


        public Button absButton;
        public Button tcsButton;
        public Button escButton;
        public Button aeroButton;
        public Button damageButton;
        public Button repairButton;

        public Button resetButton;
        public Button helpButton;
        public Button settingsButton;
        public Button telemetryButton;

        public Slider throttleSlider;
        public Slider brakeSlider;
        public Slider clutchSlider;
        public Slider handbrakeSlider;
        public Slider horizontalLeftSlider;
        public Slider horizontalRightSlider;
        public Slider damageSlider;

        public Text turboBoostTitle;
        public Text turboBoostReadout;

        private VehicleController _vc;
        private VehicleController _prevVc;

        private TrailerHitchModule _trailerHitchModule;
        private FlipOverModule _flipOverModule;
        private ABSModule _absModule;
        private TCSModule _tcsModule;
        private ESCModule _escModule;
        private AerodynamicsModule _aeroModule;

        private ColorBlock _colorBlock;
        private Canvas _canvas;

        private bool _toggleGUI;


        private void Start()
        {
            absButton.onClick.AddListener(ToggleABS);
            tcsButton.onClick.AddListener(ToggleTCS);
            escButton.onClick.AddListener(ToggleESC);
            aeroButton.onClick.AddListener(ToggleAero);
            damageButton.onClick.AddListener(ToggleDamage);
            repairButton.onClick.AddListener(RepairDamage);

            helpButton.onClick.AddListener(ToggleHelpWindow);
            telemetryButton.onClick.AddListener(ToggleTelemetryWindow);
            settingsButton.onClick.AddListener(ToggleSettingsWindow);

            resetButton.onClick.AddListener(ResetScene);

            _canvas = GetComponent<Canvas>();
        }


        private void Update()
        {
            if (VehicleChanger.Instance == null) return;

            _vc = VehicleChanger.ActiveVehicle as VehicleController;

            promptText.text = "";

            _toggleGUI = InputProvider.CombinedInput<SceneInputProviderBase>(i => i.ToggleGUI());
            if (_toggleGUI)
            {
                _canvas.enabled = !_canvas.enabled;
            }

            if (VehicleChanger.Instance.location == VehicleChanger.CharacterLocation.Near)
            {
                promptText.text += "Press V / Select (Xbox/PS) to enter the vehicle.\r\n";
            }
            else if (VehicleChanger.Instance.location == VehicleChanger.CharacterLocation.Inside
                && _vc.Speed < VehicleChanger.Instance.maxEnterExitVehicleSpeed)
            {
                promptText.text += "Press V / Select (Xbox/PS) to exit/change the vehicle.\r\n";
            }


            if (_vc == null)
            {
                return;
            }

            if (_vc != _prevVc)
            {
                _trailerHitchModule = _vc.moduleManager.GetModule<TrailerHitchModule>();
                _flipOverModule = _vc.moduleManager.GetModule<FlipOverModule>();
                _absModule = _vc.moduleManager.GetModule<ABSModule>();
                _tcsModule = _vc.moduleManager.GetModule<TCSModule>();
                _escModule = _vc.moduleManager.GetModule<ESCModule>();
                _aeroModule = _vc.moduleManager.GetModule<AerodynamicsModule>();
            }

            throttleSlider.value = Mathf.Clamp01(_vc.input.InputSwappedThrottle);
            brakeSlider.value = Mathf.Clamp01(_vc.input.InputSwappedBrakes);
            clutchSlider.value = Mathf.Clamp01(_vc.powertrain.clutch.clutchEngagement);
            handbrakeSlider.value = Mathf.Clamp01(_vc.input.states.handbrake);
            horizontalLeftSlider.value = Mathf.Clamp01(-_vc.input.Steering);
            horizontalRightSlider.value = Mathf.Clamp01(_vc.input.Steering);

            if (_trailerHitchModule != null && _trailerHitchModule.trailerInRange && !_trailerHitchModule.attached)
            {
                promptText.text += "Press T / X (Xbox) / Square (PS) to attach the trailer.\r\n";
            }

            if (_flipOverModule != null && _flipOverModule.flipOverActivation == FlipOverModule.FlipOverActivation.Manual
                && _flipOverModule.flippedOver)
            {
                promptText.text += "Press M to recover the vehicle.\r\n";
            }


            if (_absModule != null)
            {
                absButton.targetGraphic.color = _absModule.IsEnabled ? enabledColor : disabledColor;
            }

            if (_tcsModule != null)
            {
                tcsButton.targetGraphic.color = _tcsModule.IsEnabled ? enabledColor : disabledColor;
            }

            if (_escModule != null)
            {
                escButton.targetGraphic.color = _escModule.IsEnabled ? enabledColor : disabledColor;
            }

            if (_aeroModule != null)
            {
                aeroButton.targetGraphic.color = _aeroModule.IsEnabled ? enabledColor : disabledColor;
            }

            if (turboBoostTitle != null)
            {
                if (_vc.powertrain.engine.forcedInduction.useForcedInduction)
                {
                    turboBoostTitle.text = "Turbo Boost";
                    turboBoostReadout.text = (_vc.powertrain.engine.forcedInduction.boost * 100f).ToString("F0") + " %";
                }
                else
                {
                    turboBoostTitle.text = "";
                    turboBoostReadout.text = "";
                }
            }

            damageButton.targetGraphic.color = _vc.damageHandler.Active ? enabledColor : disabledColor;
            damageSlider.value = _vc.damageHandler.Damage;

            _prevVc = _vc;
        }


        public void ToggleDamage()
        {
            _vc.damageHandler.LodIndex = -1;
            _vc.damageHandler.ToggleState();
        }


        public void RepairDamage()
        {
            if (_vc != null && _vc.damageHandler.Active)
            {
                _vc.damageHandler.Repair();
            }
        }


        public void ToggleAero()
        {
            if (_aeroModule != null)
            {
                _aeroModule.LodIndex = -1;
                _aeroModule.ToggleState();
            }
        }


        public void ToggleABS()
        {
            if (_absModule != null)
            {
                _absModule.LodIndex = -1;
                _absModule.ToggleState();
            }
        }


        public void ToggleTCS()
        {
            if (_tcsModule != null)
            {
                _tcsModule.LodIndex = -1;
                _tcsModule.ToggleState();
            }
        }


        public void ToggleESC()
        {
            if (_escModule != null)
            {
                _escModule.LodIndex = -1;
                _escModule.ToggleState();
            }
        }

        public void ToggleHelpWindow()
        {
            helpWindow.SetActive(!helpWindow.activeInHierarchy);
            settingsWindow.SetActive(false);
            telemetryWindow.SetActive(false);
        }


        public void ToggleSettingsWindow()
        {
            settingsWindow.SetActive(!settingsWindow.activeInHierarchy);
            helpWindow.SetActive(false);
            telemetryWindow.SetActive(false);
        }


        public void ToggleTelemetryWindow()
        {
            telemetryWindow.SetActive(!telemetryWindow.activeInHierarchy);
            settingsWindow.SetActive(false);
            helpWindow.SetActive(false);
        }


        public void ResetScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}