using NWH.Common.Vehicles;
using UnityEngine;
using UnityEngine.UI;

namespace NWH.VehiclePhysics2.Demo.VehicleOverview
{
    public class WheelUI : MonoBehaviour
    {
        public WheelUAPI wheelUAPI;

        public Image bgImage;
        public Slider loadSlider;
        public Slider lngSlipSlider;
        public Slider latSlipSlider;
        public Slider torqueSlider;

        public Color noTorqueColor;
        public Color maxTorqueColor;

        private float _smoothTorque;
        private float _smoothLoad;
        private float _smoothLatSlip;
        private float _smoothLngSlip;

        private float _maxTorque;
        private VehicleController _vc;


        public void Start()
        {
            _vc = VehicleOverview.Instance.vc;
            _maxTorque = 9600f * _vc.powertrain.engine.maxPower / _vc.powertrain.engine.maxRPM *
                         _vc.powertrain.transmission.ForwardGears[0] *
                         _vc.powertrain.transmission.finalGearRatio;
        }


        public void Update()
        {
            float dt = Time.deltaTime;
            _smoothTorque = Mathf.Lerp(_smoothTorque, Mathf.Abs(wheelUAPI.MotorTorque), dt * 40f);
            _smoothLoad = Mathf.Lerp(_smoothLoad, wheelUAPI.Load, dt * 40f);
            _smoothLatSlip = Mathf.Lerp(_smoothLatSlip, Mathf.Abs(wheelUAPI.LateralSlip), dt * 40f);
            _smoothLngSlip = Mathf.Lerp(_smoothLngSlip, Mathf.Abs(wheelUAPI.LongitudinalSlip), dt * 40f);

            bgImage.color = Color.Lerp(noTorqueColor, maxTorqueColor, _smoothTorque / _maxTorque);
            loadSlider.value = Mathf.Clamp01(_smoothLoad / wheelUAPI.MaxLoad);
            lngSlipSlider.value = Mathf.Clamp01(_smoothLngSlip / 1f);
            latSlipSlider.value = Mathf.Clamp01(_smoothLatSlip / 1f);
            torqueSlider.value = Mathf.Clamp01(_smoothTorque / wheelUAPI.MaxLoad);
        }
    }
}