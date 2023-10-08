using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NWH.Common.SceneManagement;
using NWH.Common.Vehicles;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using UnityEngine;
using UnityEngine.UI;

namespace NWH.VehiclePhysics2.Demo
{
    public class DemoVehicleSettings : MonoBehaviour
    {
        public List<Setting> settingList = new List<Setting>();
        public GameObject settingPrefab;
        private Font font;

        private void Start()
        {
            font = Resources.Load<Font>("NWH/Fonts/Inconsolata/Inconsolata-Regular") as Font;

            if (VehicleChanger.Instance == null)
            {
                Debug.LogError("DemoVehicleSettings requires VehicleChanger in the scene to work.");
                return;
            }

            VehicleChanger.Instance.onVehicleChanged.AddListener(HandleVehicleChange);

            Redraw();
        }


        private void HandleVehicleChange()
        {
            Clear();

            if (VehicleChanger.Instance == null || VehicleChanger.ActiveVehicle == null)
            {
                return;
            }

            Redraw();
        }


        private void Update()
        {
            if (VehicleChanger.Instance == null || VehicleChanger.ActiveVehicle == null)
            {
                return;
            }

            foreach (Setting setting in settingList)
            {
                if (setting.valueField != null)
                {
                    setting.valueField.text = setting.field.GetValue(setting.obj).ToString();
                }
            }
        }


        private void Redraw()
        {
            VehicleController vc = VehicleChanger.ActiveVehicle as VehicleController;
            if (vc == null)
            {
                return;
            }


            AddTitle("==== CONTROL ====");

            AddTitle("Steering");
            AddSettings(vc.steering);

            AddTitle("Input");
            AddSettings(vc.input);

            AddTitle("Brakes");
            AddSettings(vc.brakes);

            AddTitle("==== POWERTRAIN ====");

            AddTitle("Engine");
            AddSettings(vc.powertrain.engine);

            AddTitle("Engine.ForcedInduction");
            AddSettings(vc.powertrain.engine.forcedInduction);

            AddTitle("Clutch");
            AddSettings(vc.powertrain.clutch);

            AddTitle("Transmission");
            AddSettings(vc.powertrain.transmission);
            AddSettings(vc.powertrain.transmission.gearingProfile);

            AddTitle("Differentials");
            for (int i = 0; i < vc.powertrain.differentials.Count; i++)
            {
                AddSettings(vc.powertrain.differentials[i]);
            }

            // Display axles
            for (int i = 0; i < vc.powertrain.wheelGroups.Count; i++)
            {
                WheelGroup axle = vc.powertrain.wheelGroups[i];

                AddTitle("Axle " + i);
                AddSettings(axle);

                if (axle.Wheels.Count == 1)
                {
                    AddTitle("Wheel, Axle " + i, true);
                    WheelUAPI wheelUAPI = axle.Wheels[0].wheelUAPI;
                    AddSettings(wheelUAPI);
                }
                else if (axle.Wheels.Count == 2)
                {
                    AddTitle("Left Wheel, Axle " + i, true);
                    WheelUAPI leftWheelController = axle.LeftWheel.wheelUAPI;
                    AddSettings(leftWheelController);

                    AddTitle("Right Wheel, Axle " + i, true);
                    WheelUAPI rightWheelController = axle.RightWheel.wheelUAPI;
                    AddSettings(rightWheelController);
                }
            }

            AddTitle("Steering");
            AddSettings(vc.steering);

            AddTitle("Brakes");
            AddSettings(vc.brakes);

            // Display modules
            AddTitle("Modules:");

            for (int i = 0; i < vc.moduleManager.modules.Count; i++)
            {
                AddTitle(vc.moduleManager.modules[i].GetType().Name);
                AddSettings(vc.moduleManager.modules[i]);
            }
        }


        public void Clear()
        {
            foreach (Setting setting in settingList)
            {
                Destroy(setting.settingObject);
            }

            settingList.Clear();
        }


        private void AddSettings(object obj)
        {
            foreach (FieldInfo field in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                                                BindingFlags.GetField |
                                                                BindingFlags.GetProperty | BindingFlags.Instance))
            {
                if (!field.IsDefined(typeof(ShowInSettings), false))
                {
                    continue;
                }

                AddSetting(field, obj);
            }
        }


        public void AddTitle(string text, bool subtitle = false)
        {
            GameObject title = new GameObject();
            title.name = text;
            Text titleText = title.AddComponent<Text>();
            titleText.font = font;
            titleText.text = text;
            titleText.fontSize = 15;
            titleText.alignment = TextAnchor.MiddleLeft;
            if (!subtitle)
            {
                titleText.fontStyle = FontStyle.Bold;
            }

            title.transform.SetParent(gameObject.transform, false);
            RectTransform titleRT = title.GetComponent<RectTransform>();
            titleRT.sizeDelta = new Vector2(260, 25);
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(0, 1);
            settingList.Add(new Setting { settingObject = title, });
        }


        public void AddSetting(FieldInfo field, object obj)
        {
            Setting setting = new Setting();
            setting.field = field;
            setting.obj = obj;
            setting.settingObject = Instantiate(settingPrefab, gameObject.transform, false);
            setting.settingObject.name = field.Name + "Setting";

            setting.nameField = setting.settingObject.transform.GetChild(1).GetComponent<Text>();
            setting.valueField = setting.settingObject.transform.GetChild(2).GetComponent<Text>();
            setting.leftButton = setting.settingObject.transform.GetChild(3).GetComponent<Button>();
            setting.rightButton = setting.settingObject.transform.GetChild(4).GetComponent<Button>();

            setting.nameField.text = field.Name;

            ShowInSettings attribute = field.GetCustomAttributes(typeof(ShowInSettings), false).Cast<ShowInSettings>()
                                            .FirstOrDefault();
            if (attribute == null)
            {
                return;
            }

            if (attribute.name != null)
            {
                setting.nameField.text = attribute.name;
            }

            if (field.FieldType == typeof(float))
            {
                setting.valueField.text = ((float)field.GetValue(obj)).ToString("0.00");
                setting.min = attribute.min;
                setting.max = attribute.max;
                setting.step = attribute.step;

                setting.leftButton.onClick.AddListener(() => { IncrementFloat(setting, false); });
                setting.rightButton.onClick.AddListener(() => { IncrementFloat(setting, true); });
            }
            else if (field.FieldType == typeof(int))
            {
                setting.valueField.text = field.GetValue(obj).ToString();
                setting.min = (int)attribute.min;
                setting.max = (int)attribute.max;
                setting.step = (int)attribute.step;

                setting.leftButton.onClick.AddListener(() => { IncrementInt(setting, false); });
                setting.rightButton.onClick.AddListener(() => { IncrementInt(setting, true); });
            }
            else if (field.FieldType == typeof(bool))
            {
                setting.valueField.text = field.GetValue(obj).ToString();

                setting.leftButton.onClick.AddListener(() => { ToggleBool(setting); });
                setting.rightButton.onClick.AddListener(() => { ToggleBool(setting); });
            }
            else if (field.FieldType.IsEnum)
            {
                Type enumType = field.FieldType;
                setting.min = 0;
                setting.max = enumType.GetFields(BindingFlags.Public | BindingFlags.Static).Length - 1;
                setting.step = 1;

                setting.leftButton.onClick.AddListener(() => { IncrementEnum(setting, false); });
                setting.rightButton.onClick.AddListener(() => { IncrementEnum(setting, true); });
            }

            settingList.Add(setting);
        }


        public void IncrementEnum(Setting setting, bool increment)
        {
            float currentValue = (int)setting.field.GetValue(setting.obj);
            currentValue += (int)setting.step * (increment ? 1 : -1);
            if (currentValue < 0)
            {
                currentValue = (int)setting.max;
            }
            else if (currentValue >= setting.max)
            {
                currentValue = 0;
            }

            object value = currentValue;
            value = Enum.Parse(setting.field.FieldType, value.ToString());
            setting.field.SetValue(setting.obj, value);
        }


        public void ToggleBool(Setting setting)
        {
            bool currentValue = (bool)setting.field.GetValue(setting.obj);
            currentValue = !currentValue;
            setting.field.SetValue(setting.obj, currentValue);
        }


        public void IncrementFloat(Setting setting, bool increment)
        {
            float currentValue = (float)setting.field.GetValue(setting.obj);
            currentValue = Mathf.Clamp(currentValue + setting.step * (increment ? 1f : -1f), setting.min, setting.max);
            setting.field.SetValue(setting.obj, currentValue);
        }


        public void IncrementInt(Setting setting, bool increment)
        {
            float currentValue = (int)setting.field.GetValue(setting.obj);
            currentValue = Mathf.Clamp(currentValue + (int)setting.step * (increment ? 1 : -1), (int)setting.min,
                                       (int)setting.max);
            setting.field.SetValue(setting.obj, currentValue);
        }


        public class Setting
        {
            public FieldInfo field;
            public object obj;
            public Text nameField;
            public Text valueField;
            public Button leftButton;
            public Button rightButton;
            public GameObject settingObject;
            public float min;
            public float max;
            public float step;
        }
    }
}