using System;
using System.Collections.Generic;
using System.Linq;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using NWH.Common.Vehicles;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Powertrain
{
    [Serializable]
    public partial class Powertrain : VehicleComponent
    {
        public EngineComponent engine = new EngineComponent();
        public TransmissionComponent transmission = new TransmissionComponent();
        public ClutchComponent clutch = new ClutchComponent();
        public List<DifferentialComponent> differentials = new List<DifferentialComponent>();
        public List<WheelGroup> wheelGroups = new List<WheelGroup>();
        public List<WheelComponent> wheels = new List<WheelComponent>();

        [NonSerialized]
        public List<PowertrainComponent> components = new List<PowertrainComponent>();

        public List<PowertrainComponent> Components
        {
            get { return components; }
        }


        public override void Initialize()
        {
            GetPowertrainComponents(ref components);

            foreach (PowertrainComponent pc in components)
            {
                pc.FindOutputs(this);
            }

            foreach (PowertrainComponent pc in components)
            {
                pc.FindInput(this);
            }

            foreach (PowertrainComponent component in components)
            {
                component.Initialize(vc);
            }

            foreach (WheelGroup wheelGroup in wheelGroups)
            {
                wheelGroup.Initialize(vc);
            }

            foreach (WheelComponent wheelComponent in wheels)
            {
                wheelComponent.wheelUAPI.IsVisualOnly = vc.GetMultiplayerIsRemote();
            }

            // Warnings
            if (clutch.engagementRPM < engine.idleRPM * 1.1f)
            {
                Debug.LogWarning($"Clutch engagement RPM is too low on vehicle {vc.name}. Clutch might stay engaged while in idle. Increase clutch" +
                                 " engagement RPM to be larger than engine idle RPM.");
            }

            base.Initialize();
        }


        public override void FixedUpdate()
        {
            int wheelGroupCount = wheelGroups.Count;
            for (int i = 0; i < wheelGroupCount; i++)
            {
                wheelGroups[i].ManualUpdate();
            }

            if (!Active)
            {
                return;
            }

            // ***** POWERTRAIN PHYSICS STEP *****
            int componentCount = components.Count;
            for (int i = 0; i < componentCount; i++)
            {
                components[i].OnPrePhysicsStep(vc.fixedDeltaTime);
            }

            engine.IntegrateDownwards(vc.fixedDeltaTime);

            for (int i = 0; i < componentCount; i++)
            {
                components[i].OnPostPhysicsStep(vc.fixedDeltaTime);
            }
        }


        public override void Enable()
        {
            base.Enable();

            for (int i = 0; i < components.Count; i++)
            {
                PowertrainComponent pc = components[i];
                pc.OnEnable();
            }
        }


        public override void Disable()
        {
            base.Disable();

            for (int i = 0; i < components.Count; i++)
            {
                PowertrainComponent pc = components[i];
                pc.OnDisable();
            }
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            engine.name = "Engine";
            clutch.name = "Clutch";
            transmission.name = "Transmission";

            engine.SetDefaults(vc);
            clutch.SetDefaults(vc);
            transmission.SetDefaults(vc);

            foreach (DifferentialComponent diff in differentials)
            {
                diff.SetDefaults(vc);
            }

            foreach (WheelComponent wheelComponent in wheels)
            {
                wheelComponent.SetDefaults(vc);
            }

            // Find wheels
            wheels = new List<WheelComponent>();
            WheelUAPI[] wheelUAPIs = vc.GetComponentsInChildren<WheelUAPI>();
            for (int i = 0; i < wheelUAPIs.Length; i++)
            {
                WheelUAPI wheelUAPI = wheelUAPIs[i];
                Debug.Log($"VehicleController setup: Found wheel '{wheelUAPI.transform.name}'");

                Type wheelType = wheelUAPI.GetType();
                bool usingWheelCollider = wheelType == typeof(WheelCollider);

                WheelComponent wheel = new WheelComponent();
                wheel.name = "Wheel" + wheelUAPI.transform.name;
                wheel.wheelUAPI = wheelUAPI;
                wheels.Add(wheel);
            }

            if (wheels.Count == 0)
            {
                Debug.LogWarning("No WheelControllers found, skipping powertrain auto-setup.");
                return;
            }

            // Order wheels in left-right, front to back order.
            wheels = wheels.OrderByDescending(w => w.wheelUAPI.transform.localPosition.z).ToList();
            List<int> wheelGroupIndices = new List<int>();
            int wheelGroupCount = 1;
            float prevWheelZ = wheels[0].wheelUAPI.transform.localPosition.z;
            for (int i = 0; i < wheels.Count; i++)
            {
                WheelComponent wheel = wheels[i];
                float wheelZ = wheel.wheelUAPI.transform.localPosition.z;

                // Wheels are on different axes, add new axis/wheel group.
                if (Mathf.Abs(wheelZ - prevWheelZ) > 0.2f)
                {
                    wheelGroupCount++;
                }
                // Wheels are on the same axle, order left to right.
                else if (i > 0)
                {
                    if (wheels[i].wheelUAPI.transform.localPosition.x <
                        wheels[i - 1].wheelUAPI.transform.localPosition.x)
                    {
                        WheelComponent tmp = wheels[i - 1];
                        wheels[i - 1] = wheels[i];
                        wheels[i] = tmp;
                    }
                }

                wheelGroupIndices.Add(wheelGroupCount - 1);
                prevWheelZ = wheelZ;
            }

            // Add wheel groups
            wheelGroups = new List<WheelGroup>();
            for (int i = 0; i < wheelGroupCount; i++)
            {
                string appendix = i == 0 ? "Front" : i == wheelGroupCount - 1 ? "Rear" : "Middle";
                string groupName = $"{appendix} Axle {i}";
                wheelGroups.Add(new WheelGroup
                {
                    name = groupName,
                    brakeCoefficient = i == 0 || wheelGroupCount > 2 ? 1f : 0.7f,
                    handbrakeCoefficient = i == wheelGroupCount - 1 ? 1f : 0f,
                    steerCoefficient = i == 0 ? 1f : i == 1 && wheelGroupCount > 2 ? 0.5f : 0f,
                    addAckerman = true,
                    isSolid = false,
                }); ;
                Debug.Log($"VehicleController setup: Creating WheelGroup '{groupName}'");
            }

            // Add differentials
            differentials = new List<DifferentialComponent>();
            Debug.Log("[Powertrain] Adding 'Front Differential'");
            differentials.Add(new DifferentialComponent { name = "Front Differential", });
            Debug.Log("[Powertrain] Adding 'Rear Differential'");
            differentials.Add(new DifferentialComponent { name = "Rear Differential", });
            Debug.Log("[Powertrain] Adding 'Center Differential'");
            differentials.Add(new DifferentialComponent { name = "Center Differential", });
            differentials[2].SetOutput(differentials[0], differentials[1]);

            // Connect transmission to differentials
            Debug.Log($"[Powertrain] Setting transmission output to '{differentials[2].name}'");
            transmission.SetOutput(differentials[2]);

            // Add wheels to wheel groups
            for (int i = 0; i < wheels.Count; i++)
            {
                int wheelGroupIndex = wheelGroupIndices[i];
                wheels[i].wheelGroupSelector = new WheelGroupSelector { index = wheelGroupIndex, };
                Debug.Log($"[Powertrain] Adding '{wheels[i].name}' to '{wheelGroups[wheelGroupIndex].name}'");
            }

            // Connect wheels to differentials
            int diffCount = differentials.Count;
            int wheelGroupsCount = wheelGroups.Count;
            wheelGroupsCount =
                wheelGroupCount > 2
                    ? 2
                    : wheelGroupCount; // Prevent from resetting diffs on vehicles with more than 2 axles
            for (int i = 0; i < wheelGroupsCount; i++)
            {
                WheelGroup group = wheelGroups[i];
                List<WheelComponent> belongingWheels = group.FindWheelsBelongingToGroup(ref wheels, i);

                if (belongingWheels.Count == 2)
                {
                    Debug.Log(
                        $"[Powertrain] Setting output of '{differentials[i].name}' to '{belongingWheels[0].name}'");
                    if (belongingWheels[0].wheelUAPI.transform.position.x < -0.01f)
                    {
                        differentials[i].SetOutput(belongingWheels[0], belongingWheels[1]);
                    }
                    else if (belongingWheels[0].wheelUAPI.transform.position.x > 0.01f)
                    {
                        differentials[i].SetOutput(belongingWheels[1], belongingWheels[0]);
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[Powertrain] Powertrain settings for center wheels have to be manually set up. If powered either connect it directly to transmission (motorcycle) or to one side of center differential (trike).");
                    }
                }
            }
        }


        public override void Validate(VehicleController vc)
        {
            base.Validate(vc);

            engine.Validate(vc);
            clutch.Validate(vc);
            transmission.Validate(vc);

            foreach (DifferentialComponent diff in differentials)
            {
                diff.Validate(vc);
            }

            foreach (WheelComponent wheelComponent in wheels)
            {
                wheelComponent.Validate(vc);
            }
        }


        public void AddComponent(PowertrainComponent i)
        {
            components.Add(i);
        }


        public PowertrainComponent GetComponent(int index)
        {
            if (index < 0 || index >= components.Count)
            {
                Debug.LogError("Component index out of bounds.");
                return null;
            }

            return components[index];
        }


        public PowertrainComponent GetComponent(string name)
        {
            return components.FirstOrDefault(c => c.name == name);
        }


        public List<string> GetComponentNames()
        {
            return components.Select(c => c.name).ToList();
        }


        public void RemoveComponent(PowertrainComponent i)
        {
            components.Remove(i);
        }


        public void GetPowertrainComponentNames(ref List<PowertrainComponent> powertrainComponents, ref List<string> outNames)
        {
            outNames = powertrainComponents.Select(c => c.name).ToList();
        }


        public void GetPowertrainComponentNames(ref List<string> outNames)
        {
            List<PowertrainComponent> powertrainComponents = new List<PowertrainComponent>();
            GetPowertrainComponents(ref powertrainComponents);
            GetPowertrainComponentNames(ref powertrainComponents, ref outNames);
        }


        public void GetPowertrainComponents(ref List<PowertrainComponent> powertrainComponents)
        {
            if (powertrainComponents == null)
            {
                powertrainComponents = new List<PowertrainComponent>();
            }
            else
            {
                powertrainComponents.Clear();
            }

            powertrainComponents.Clear();
            powertrainComponents.Add(engine);
            powertrainComponents.Add(clutch);
            powertrainComponents.Add(transmission);

            for (int i = 0; i < differentials.Count; i++)
            {
                DifferentialComponent differential = differentials[i];
                powertrainComponents.Add(differential);
            }

            for (int i = 0; i < wheels.Count; i++)
            {
                WheelComponent wheelComponent = wheels[i];
                powertrainComponents.Add(wheelComponent);
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Powertrain
{
    [CustomPropertyDrawer(typeof(Powertrain))]
    public partial class PowertrainDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            int powertrainTab = drawer.HorizontalToolbar("powertrainTab",
                                                         new[]
                                                         {
                                                             "Engine", "Clutch", "Transmission", "Differentials",
                                                             "Wheels", "Wheel Groups",
                                                         }, true, true);

            OutputSelectorDrawer.RefreshOutputs();

            switch (powertrainTab)
            {
                case 0:
                    drawer.Property("engine");
                    break;
                case 1:
                    drawer.Property("clutch");
                    break;
                case 2:
                    drawer.Property("transmission");
                    break;
                case 3:
                    drawer.ReorderableList("differentials", null, true, true, null, 5f);
                    break;
                case 4:
                    drawer.Space(3);
                    drawer.Info(
                        "Make sure that wheels are added in left to right, front to back order. E.g.: FrontLeft, FrontRight, RearLeft, RearRight.",
                        MessageType.Warning);
                    drawer.ReorderableList("wheels", null, true, true, null, 5f);
                    break;
                case 5:
                    drawer.ReorderableList("wheelGroups", null, true, true, null, 5f);
                    break;
            }

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
