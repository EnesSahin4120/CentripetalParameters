using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     ScriptableObject that contains a list of all the initial states for all the available VehicleComponents.
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "NWH Vehicle Physics 2", menuName = "NWH/Vehicle Physics 2/State Settings", order = 1)]
    public partial class StateSettings : ScriptableObject
    {
        public List<StateDefinition> definitions = new List<StateDefinition>();
        public List<LOD> LODs = new List<LOD>();


        public StateDefinition GetDefinition(string fullComponentTypeName)
        {
            return definitions.Find(d => d.fullName == fullComponentTypeName);
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public void Reload()
        {
            Debug.Log("Reload definitions.");
            List<string> fullNames = Assembly.GetAssembly(typeof(VehicleComponent)).GetTypes()
                                             .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(VehicleComponent)))
                                             .Select(t => t.FullName).ToList();

            foreach (string fullName in fullNames)
            {
                if (GetDefinition(fullName) == null)
                {
                    definitions.Add(new StateDefinition(fullName, true, true, -1));
                }
            }

            definitions.RemoveAll(d => fullNames.All(n => n != d.fullName));

            definitions = definitions.OrderBy(d => d.fullName).ToList();
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Editor for StateSettings.
    /// </summary>
    [CustomEditor(typeof(StateSettings))]
    [CanEditMultipleObjects]
    public partial class StateSettingsEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            StateSettings stateSettings = target as StateSettings;


            drawer.BeginSubsection("LODs");
            drawer.Info("LODs must be ordered from lowest to highest distance.");
            drawer.ReorderableList("LODs", "LODs");
            drawer.EndSubsection();

            drawer.BeginSubsection("Component State Definitions");
            drawer.Info(
                "VehicleComponent state definitions are fetched on Awake() and any changes made to them will only " +
                "affect vehicles initialized after the change was made.", MessageType.Warning);
            DrawDefinitionsList(stateSettings);
            drawer.EndSubsection();

            drawer.EndEditor();
            return true;
        }


        public override bool UseDefaultMargins()
        {
            return false;
        }


        private void DrawDefinitionsList(StateSettings stateSettings)
        {
            if (drawer.Button("Refresh"))
            {
                stateSettings.Reload();

                // Save the changes after reload
                EditorUtility.SetDirty(stateSettings);
                serializedObject.ApplyModifiedProperties();
            }

            drawer.Label("Vehicle Components:");

            SerializedProperty listProperty = drawer.FindProperty("definitions");
            int n = listProperty.arraySize;
            for (int i = 0; i < n; i++)
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                if (element == null) break;
                drawer.Property(element);
            }

            drawer.Space(10);
        }
    }
}

#endif
