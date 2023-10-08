using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.Modules
{
    /// <summary>
    ///     Wrapper around modules.
    ///     Unity does not support polymorphic serializations (not counting the SerializeReference which in 2019.3 is not
    ///     production ready) so
    ///     the workaround is to wrap each module type in a MonoBehaviour wrapper.
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    [Serializable]
    public abstract class ModuleWrapper : MonoBehaviour
    {
        /// <summary>
        ///     Returns wrapper's module.
        /// </summary>
        /// <returns></returns>
        public abstract VehicleModule GetModule();


        /// <summary>
        ///     Sets wrapper's module.
        /// </summary>
        /// <param name="vehicleModule"></param>
        public abstract void SetModule(VehicleModule vehicleModule);


        private void Reset()
        {
            VehicleController vc = GetComponent<VehicleController>();
            if (vc != null)
            {
                vc.moduleManager.ReloadModulesList(vc);
                GetModule().SetDefaults(vc);
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules
{
    [CustomEditor(typeof(ModuleWrapper), true)]
    [CanEditMultipleObjects]
    public partial class ModuleWrapperEditor : NVP_NUIEditor
    {
        public override void OnInspectorGUI()
        {
            OnInspectorNUI();
        }


        public override bool OnInspectorNUI()
        {
            drawer.BeginEditor(serializedObject);
            drawer.Property(drawer.serializedObject.FindProperty("module"));
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
