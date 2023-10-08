using System;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Class storing VehicleComponent state.
    /// </summary>
    [Serializable]
    public partial class StateDefinition
    {
        public string fullName;
        public bool isEnabled = true;
        public bool isOn = true;
        public int lodIndex = -1;
        public string name;


        public StateDefinition()
        {
        }


        public StateDefinition(string fullName, bool isOn, bool isEnabled, int lod)
        {
            this.fullName = fullName;
            name = fullName.Split('.').Last();
            this.isOn = isOn;
            this.isEnabled = isEnabled;
            lodIndex = lod;
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Custom property drawer for StateDefinition.
    /// </summary>
    [CustomPropertyDrawer(typeof(StateDefinition))]
    public partial class StateDefinitionDrawer : NVP_NUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            drawer.BeginProperty(position, property, label);

            // Draw label
            string fullName = drawer.FindProperty("fullName").stringValue.Replace("NWH.VehiclePhysics2.", "");
            string shortName = fullName.Split('.').Last();

            GUIStyle miniStyle = EditorStyles.centeredGreyMiniLabel;
            miniStyle.alignment = TextAnchor.MiddleLeft;

            Rect labelRect = drawer.positionRect;
            labelRect.x += 5f;

            Rect miniRect = drawer.positionRect;
            miniRect.x += 200f;

            EditorGUI.LabelField(labelRect, shortName, EditorStyles.boldLabel);
            EditorGUI.LabelField(miniRect, fullName, miniStyle);
            drawer.Space(NUISettings.fieldHeight);

            StateSettings stateSettings =
                SerializedPropertyHelper.GetTargetObjectWithProperty(property) as StateSettings;
            if (stateSettings == null)
            {
                drawer.EndProperty();
                return false;
            }
            bool isOn = property.FindPropertyRelative("isOn").boolValue;
            bool isEnabled = property.FindPropertyRelative("isEnabled").boolValue;
            int lodIndex = property.FindPropertyRelative("lodIndex").intValue;

            bool wasOn = isOn;
            bool wasEnabled = isEnabled;
            int prevLodIndex = lodIndex;

            ComponentNUIPropertyDrawer.DrawStateSettingsBar(
                position,
                String.Empty,
                stateSettings.LODs.Count,
                ref isOn,
                ref isEnabled,
                ref lodIndex);

            property.FindPropertyRelative("isOn").boolValue = isOn;
            property.FindPropertyRelative("isEnabled").boolValue = isEnabled;
            property.FindPropertyRelative("lodIndex").intValue = lodIndex;

            if (isOn != wasOn || wasEnabled != isEnabled || prevLodIndex != lodIndex)
            {
                property.serializedObject.ApplyModifiedProperties();
            }

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
