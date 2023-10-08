using System.Reflection;
using NWH.Common;
using NWH.Common.SceneManagement;
using NWH.Common.Vehicles;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using UnityEngine;
using UnityEngine.UI;

namespace NWH.VehiclePhysics2.Demo
{
    public class DemoTelemetry : MonoBehaviour
    {
        public Text textUI;
        private string text;
        private VehicleController vc;


        private void LateUpdate()
        {
            if (Time.frameCount % 5 != 0)
            {
                return;
            }

            vc = VehicleChanger.ActiveVehicle as VehicleController;
            if (vc == null)
            {
                return;
            }

            // Build strings
            AddTitle("Vehicle");
            PrintProperties(vc);

            AddTitle("Steering");
            PrintProperties(vc.steering);

            AddTitle("Engine");
            PrintProperties(vc.powertrain.engine);
            AddTitle("Forced Induction", ' ');
            PrintProperties(vc.powertrain.engine.forcedInduction);
            AddSpace();

            AddTitle("Clutch");
            PrintProperties(vc.powertrain.clutch);
            AddSpace();

            AddTitle("Transmission");
            PrintProperties(vc.powertrain.transmission);
            AddSpace();


            AddTitle("Axles");
            int count = 0;
            foreach (WheelGroup axle in vc.powertrain.wheelGroups)
            {
                AddTitle("Axle " + count);
                PrintProperties(axle);

                AddTitle("Left Wheel", ' ');
                PrintProperties(axle.LeftWheel);
                PrintProperties(axle.LeftWheel.wheelUAPI);

                AddTitle("Right Wheel:", ' ');
                PrintProperties(axle.RightWheel);
                PrintProperties(axle.RightWheel.wheelUAPI);
                count++;
            }

            textUI.text = text;
            text = "";
        }


        private void PrintProperties(object obj, string prefix = "")
        {
            foreach (FieldInfo field in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                                                BindingFlags.GetField |
                                                                BindingFlags.GetProperty | BindingFlags.Instance))
            {
                if (!field.IsDefined(typeof(ShowInTelemetry), false))
                {
                    continue;
                }

                if (field.FieldType == typeof(float))
                {
                    string value = ((float)field.GetValue(obj)).ToString("0.00");
                    AddLine(prefix + field.Name, value);
                }
                else
                {
                    try
                    {
                        AddLine(prefix + field.Name, field.GetValue(obj).ToString());
                    }
                    catch
                    {
                    }

                    ;
                }
            }

            foreach (PropertyInfo property in obj.GetType().GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField |
                BindingFlags.GetProperty | BindingFlags.Instance))
            {
                if (!property.IsDefined(typeof(ShowInTelemetry), false))
                {
                    continue;
                }

                if (property.PropertyType == typeof(float))
                {
                    string value = ((float)property.GetValue(obj, null)).ToString("0.00");
                    AddLine(property.Name, value);
                }
                else
                {
                    try
                    {
                        AddLine(property.Name, property.GetValue(obj, null).ToString());
                    }
                    catch
                    {
                    }

                    ;
                }
            }
        }


        private void AddLine(string name, string value = "")
        {
            name = Truncate(name, 23);
            text += string.Format("{0,-26}{1,14}", ChangeCase(name), value) + "\n";
        }


        private void AddLine(string name, float value)
        {
            string stringValue = value.ToString("0.0");
            AddLine(name, stringValue);
        }


        private void AddTitle(string title, char filler = '_')
        {
            text += "\n" + CenterString(title, 40, filler);
        }


        private void AddSpace()
        {
            text += "\n";
        }


        private string CenterString(string stringToCenter, int totalLength, char filler)
        {
            return stringToCenter.PadLeft((totalLength - stringToCenter.Length) / 2
                                          + stringToCenter.Length, filler)
                                 .PadRight(totalLength, filler) + "\n";
        }


        public string Truncate(string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "..";
        }


        public static string ChangeCase(string str)
        {
            if (str == null)
            {
                return null;
            }

            if (str.Length > 1)
            {
                return char.ToUpper(str[0]) + str.Substring(1);
            }

            return str.ToUpper();
        }
    }
}