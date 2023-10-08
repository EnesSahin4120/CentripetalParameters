using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Powertrain
{
    [Serializable]
    public partial class OutputSelector
    {
        public string name;
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Powertrain
{
    [CustomPropertyDrawer(typeof(OutputSelector))]
    public partial class OutputSelectorDrawer : PropertyDrawer
    {
        public int selectedIndex;
        public SerializedProperty socketNameProperty;

        public static VehicleController vc;
        public static string[] options = null;
        public static List<PowertrainComponent> components;


        public static void RefreshOutputs()
        {
            if (vc == null || vc.powertrain == null) return;

            if (components == null)
            {
                components = new List<PowertrainComponent>();
            }
            else
            {
                components.Clear();
            }

            vc.powertrain.GetPowertrainComponents(ref components);

            // Add the names of other powertrain components
            if (options == null || options.Length - 1 != components.Count)
            {
                options = new string[components.Count + 1];
                options[0] = "[none]";
            }

            for (int i = 0; i < components.Count; i++)
            {
                PowertrainComponent c = components[i];
                options[i + 1] = $"[{c.GetType().ToString().Split('.').LastOrDefault()?.Replace("Component", "")}] {c.name}";

            }
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            vc = property.serializedObject.targetObject as VehicleController;

            if (options == null || components == null)
            {
                return;
            }

            if (options.Length == 0 || components.Count == 0)
            {
                return;
            }

            if (vc == null)
            {
                selectedIndex = EditorGUI.Popup(rect, "", 0, options);
                return;
            }

            // Find the name and index of currently selected output
            socketNameProperty = property.FindPropertyRelative("name");
            string name = socketNameProperty.stringValue;
            int index = components.FindIndex(c => c.name == name) + 1;

            if (!Application.isPlaying)
            {
                // Display dropdown menu
                selectedIndex = EditorGUI.Popup(rect, "", index, options);

                // Display currently selected output
                if (selectedIndex >= 1)
                {
                    socketNameProperty.stringValue = components[selectedIndex - 1].name;
                }
                else
                {
                    socketNameProperty.stringValue = null;
                }
            }
            else
            {
                EditorGUI.LabelField(rect, name);
            }
        }
    }
}

#endif
