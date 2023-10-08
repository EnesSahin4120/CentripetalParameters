using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.VehiclePhysics2.Effects
{
    /// <summary>
    ///     Main class for handling visual effects such as skidmarks, lights and exhausts.
    /// </summary>
    [Serializable]
    public partial class EffectManager : ManagerVehicleComponent
    {
        public ExhaustFlash exhaustFlash = new ExhaustFlash();
        public ExhaustSmoke exhaustSmoke = new ExhaustSmoke();
        [FormerlySerializedAs("lights")] public LightsMananger lightsManager = new LightsMananger();
        [FormerlySerializedAs("skidmarks")] public SkidmarkManager skidmarkManager = new SkidmarkManager();
        public SurfaceParticleManager surfaceParticleManager = new SurfaceParticleManager();

        public override List<VehicleComponent> Components
        {
            get
            {
                if (_components == null)
                {
                    _components = new List<VehicleComponent>
                    {
                        exhaustFlash,
                        exhaustSmoke,
                        lightsManager,
                        skidmarkManager,
                        surfaceParticleManager,
                    };
                }
                return _components;
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Effects
{
    [CustomPropertyDrawer(typeof(EffectManager))]
    public partial class EffectManagerDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }


            int effectsTab = drawer.HorizontalToolbar("effectsTab",
                                                      new[]
                                                      {
                                                          "Skidmarks", "Lights", "Surf. Part.", "Ex. Smoke",
                                                          "Ex. Flash",
                                                      });

            switch (effectsTab)
            {
                case 0:
                    drawer.Property("skidmarkManager");
                    break;
                case 1:
                    drawer.Property("lightsManager");
                    break;
                case 2:
                    drawer.Property("surfaceParticleManager");
                    break;
                case 3:
                    drawer.Property("exhaustSmoke");
                    break;
                case 4:
                    drawer.Property("exhaustFlash");
                    break;
                default:
                    drawer.Property("skidmarks");
                    break;
            }


            drawer.EndProperty();
            return true;
        }
    }
}

#endif
