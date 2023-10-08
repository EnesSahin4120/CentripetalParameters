using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.Modules
{
    /// <summary>
    ///     Manages vehicle modules.
    /// </summary>
    public partial class ModuleManager : VehicleComponent
    {
        /// <summary>
        ///     Vehicle modules. Only modules in this list will get updated.
        /// </summary>
        [Tooltip("    Vehicle modules. Only modules in this list will get updated.")]
        [NonSerialized]
        public List<VehicleModule> modules = new List<VehicleModule>();


        public override void Start(VehicleController vc)
        {
            base.Start(vc);

            ReloadModulesList(vc);
            for (int i = 0; i < modules.Count; i++)
            {
                modules[i].Start(vc);
            }
        }


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            for (int i = 0; i < modules.Count; i++)
            {
                modules[i].Update();
            }
        }


        public override void FixedUpdate()
        {
            if (!Active)
            {
                return;
            }

            for (int i = 0; i < modules.Count; i++)
            {
                modules[i].FixedUpdate();
            }
        }


        public override void OnDrawGizmosSelected(VehicleController vc)
        {
            if (modules == null || modules.Count == 0)
            {
                ReloadModulesList(vc);
            }

            for (int i = 0; i < vc.moduleManager.modules.Count; i++)
            {
                VehicleModule module = vc.moduleManager.modules[i];
                module.OnDrawGizmosSelected(vc);
            }
        }


        /// <summary>
        ///     Adds a module to the game object. Equivalent to using AddComponent followed by UpdateModulesList().
        ///     Can be called only in play mode, after the vehicle has been initialized.
        /// </summary>
        public TM AddModule<TW, TM>()
            where TW : ModuleWrapper
            where TM : VehicleModule
        {
            if (vc == null)
            {
                return null;
            }

            VehicleModule module = vc.gameObject.AddComponent<TW>().GetModule();
            modules.Add(module);
            ReloadModulesList(vc);
            return module as TM;
        }


        /// <summary>
        ///     Returns a module from the modules list.
        ///     Can be called only in play mode, after the vehicle has been initialized.
        /// </summary>
        public TM GetModule<TM>() where TM : VehicleModule
        {
            if (vc == null)
            {
                return null;
            }

            return modules.FirstOrDefault(m => m.GetType() == typeof(TM)) as TM;
        }


        public override void CheckState(int lodIndex)
        {
            foreach (VehicleModule module in modules)
            {
                module.CheckState(lodIndex);
            }

            base.CheckState(lodIndex);
        }


        /// <summary>
        ///     Removes the module from the object and from the modules list.
        ///     Can be called only in play mode, after the vehicle has been initialized.
        /// </summary>
        public void RemoveModule<TW>()
            where TW : ModuleWrapper
        {
            if (vc == null)
            {
                return;
            }

            ModuleWrapper wrapper = vc.gameObject.GetComponent<TW>();
            modules.Remove(wrapper.GetModule());
            if (Application.isPlaying)
            {
                Object.Destroy(wrapper);
            }
            else
            {
                Object.DestroyImmediate(wrapper);
            }

            ReloadModulesList(vc);
        }


        public void ReloadModulesList(VehicleController vc)
        {
            modules.Clear();
            ModuleWrapper[] moduleWrappers = vc.GetComponents<ModuleWrapper>();
            if (moduleWrappers.Length == 0 || moduleWrappers == null)
            {
                return;
            }

            for (int i = 0; i < moduleWrappers.Length; i++)
            {
                modules.Add(moduleWrappers[i].GetModule());
            }
        }
    }
}

#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules
{
    [CustomPropertyDrawer(typeof(ModuleManager))]
    public partial class ModuleManagerDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }


            drawer.EndProperty();
            return true;
        }
    }
}

#endif