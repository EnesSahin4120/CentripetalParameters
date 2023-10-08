using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.Rigging
{
    /// <summary>
    ///     Module used to animate rigged models by moving the axle/wheel bones.
    /// </summary>
    [Serializable]
    public partial class RiggingModule : VehicleModule
    {
        public List<Bone> bones = new List<Bone>();


        public override void Start(VehicleController vc)
        {
            base.Start(vc);

            foreach (Bone bone in bones)
            {
                bone.Initialize();
            }
        }


        public override void Update()
        {
            Vector3 forward = vc.vehicleTransform.forward;
            Vector3 up = vc.vehicleTransform.up;
            foreach (Bone bone in bones)
            {
                bone.Update(forward, up);
            }
        }
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Modules.Rigging
{
    [CustomPropertyDrawer(typeof(RiggingModule))]
    public partial class RiggingModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.ReorderableList("bones");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
