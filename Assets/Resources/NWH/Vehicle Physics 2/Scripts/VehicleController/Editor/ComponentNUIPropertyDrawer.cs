#if UNITY_EDITOR
using NWH.NUI;
using UnityEditor;
using UnityEngine;

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Used for drawing VehicleComponent properties.
    ///     Adds state header functionality to the NUIPropertyDrawer.
    /// </summary>
    public partial class ComponentNUIPropertyDrawer : NVP_NUIPropertyDrawer
    {
        public static void DrawStateSettingsBar(Rect position, string stateDefinitonsName, int lodCount, ref bool isOn,
            ref bool isEnabled, ref int lodIndex, float topOffset = 4f)
        {
            Color initialColor = GUI.backgroundColor;

            // Button style
            GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton);
            buttonStyle.fixedHeight = 15;
            buttonStyle.fontSize = 8;
            buttonStyle.padding = new RectOffset(0, 0, buttonStyle.padding.top, buttonStyle.padding.bottom);
            buttonStyle.alignment = TextAnchor.MiddleCenter;


            // DRAW isOn BUTTON
            bool guiWasEnabled = GUI.enabled;
            {
                GUI.backgroundColor = isOn ? NUISettings.enabledColor : NUISettings.disabledColor;
                string text = isOn ? "ON" : "OFF";
                Rect buttonRect = new Rect(position.x + position.width - 40f, position.y + topOffset, 35f, 17f);
                if (GUI.Button(buttonRect, text, buttonStyle))
                {
                    isOn = !isOn;
                }

                GUI.enabled = guiWasEnabled;
                GUI.backgroundColor = initialColor;

                if (!isOn)
                {
                    return;
                }
            }

            // DRAW LOD BUTTONS
            int lodIndexValue = -1;
            {
                // Draw LOD menu
                if (lodCount > 0)
                {
                    bool lodActive = lodIndex >= 0;
                    float rightOffset = -94;
                    float lodButtonWidth = 18f;
                    float lodLabelWidth = 35f;
                    float lodWidth = lodCount * lodButtonWidth;

                    /// Draw label
                    Rect lodLabelRect = new Rect(
                        position.x + position.width - lodWidth - lodLabelWidth + rightOffset,
                        position.y + topOffset, lodLabelWidth, 15f);
                    bool wasEnabled = GUI.enabled;
                    GUI.enabled = false;

                    GUIStyle lodLabelStyle = new GUIStyle(EditorStyles.miniButtonLeft);
                    lodLabelStyle.fixedHeight = 15;
                    lodLabelStyle.fontSize = 10;

                    GUI.Button(lodLabelRect, "LOD", lodLabelStyle);

                    GUI.enabled = wasEnabled;

                    // Draw lod buttons
                    initialColor = GUI.backgroundColor;
                    if (lodIndex >= 0)
                    {
                        GUI.backgroundColor = NUISettings.enabledColor;
                    }

                    GUIStyle lodButtonStyle;
                    GUIStyle middleLODButtonStyle = new GUIStyle(EditorStyles.miniButtonMid);
                    GUIStyle lastLODButtonStyle = new GUIStyle(EditorStyles.miniButtonRight);

                    middleLODButtonStyle.fixedHeight = lastLODButtonStyle.fixedHeight = 15;
                    middleLODButtonStyle.fontSize = lastLODButtonStyle.fontSize = 8;
                    middleLODButtonStyle.alignment = lastLODButtonStyle.alignment = TextAnchor.MiddleCenter;

                    for (int i = 0; i < lodCount; i++)
                    {
                        Rect lodButtonRect = new Rect(
                            position.x + position.width - lodWidth + i * lodButtonWidth +
                            rightOffset,
                            position.y + topOffset, lodButtonWidth, 15f);

                        string buttonText = i.ToString();
                        if (i == lodCount - 1)
                        {
                            buttonText = "S";
                        }

                        lodButtonStyle = i == lodCount - 1 ? lastLODButtonStyle : middleLODButtonStyle;

                        if (GUI.Button(lodButtonRect, buttonText, lodButtonStyle))
                        {
                            if (i == lodIndex)
                            {
                                lodIndex = -1;
                            }
                            else
                            {
                                lodIndex = i;
                            }
                        }

                        if (i == lodIndex)
                        {
                            GUI.backgroundColor = NUISettings.disabledColor;
                        }
                    }

                    GUI.backgroundColor = initialColor;
                }
            }


            // Draw Enabled button
            {
                guiWasEnabled = GUI.enabled;

                if (lodIndexValue < 0)
                {
                    GUI.enabled = true;
                }
                else
                {
                    GUI.enabled = false;
                }

                GUI.backgroundColor = isEnabled ? NUISettings.enabledColor : NUISettings.disabledColor;
                string text = isEnabled ? "ENABLED" : "DISABLED";
                Rect buttonRect = new Rect(position.x + position.width - 89f, position.y + topOffset, 45f, 17f);
                if (GUI.Button(buttonRect, text, buttonStyle))
                {
                    isEnabled = !isEnabled;
                }

                GUI.enabled = guiWasEnabled;
                GUI.backgroundColor = initialColor;
            }
        }


        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool isExpanded = base.OnNUI(position, property, label);

            if (property == null) return isExpanded;

            Component component = property.serializedObject.targetObject as Component;
            VehicleController vehicleController = component.gameObject.GetComponent<VehicleController>();

            if (vehicleController == null || vehicleController.stateSettings == null) return isExpanded;

            if (property.serializedObject.targetObjects.Length > 1) return isExpanded;


            // Draw state settings
            if (Application.isPlaying)
            {
                bool isOn = property.FindPropertyRelative("state.isOn").boolValue;
                bool isEnabled = property.FindPropertyRelative("state.isEnabled").boolValue;
                int lodIndex = property.FindPropertyRelative("state.lodIndex").intValue;

                bool wasOn = isOn;
                bool wasEnabled = isEnabled;
                int prevLodIndex = lodIndex;

                DrawStateSettingsBar(
                    position,
                    vehicleController.stateSettings.name,
                    vehicleController.stateSettings.LODs.Count,
                    ref isOn,
                    ref isEnabled,
                    ref lodIndex);

                property.FindPropertyRelative("state.isOn").boolValue = isOn;
                property.FindPropertyRelative("state.isEnabled").boolValue = isEnabled;
                property.FindPropertyRelative("state.lodIndex").intValue = lodIndex;

                if (isOn != wasOn || wasEnabled != isEnabled || prevLodIndex != lodIndex)
                {
                    EditorUtility.SetDirty(vehicleController.stateSettings);
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                string fullName = SerializedPropertyHelper.GetTargetObjectOfProperty(property)?.GetType()?.FullName;
                if (fullName == null) return isExpanded;
                int definitionIndex = vehicleController.stateSettings.definitions.FindIndex(d => d.fullName == fullName);
                if (definitionIndex < 0)
                {
                    //Debug.LogWarning($"State definition for {fullName} not found.");
                    return isExpanded;
                }

                StateDefinition definition = vehicleController.stateSettings.definitions[definitionIndex];

                DrawStateSettingsBar(
                    position,
                    vehicleController.stateSettings.name,
                    vehicleController.stateSettings.LODs.Count,
                    ref definition.isOn,
                    ref definition.isEnabled,
                    ref definition.lodIndex);

                vehicleController.stateSettings.definitions[definitionIndex] = definition;
            }

            return isExpanded;
        }
    }
}

#endif
