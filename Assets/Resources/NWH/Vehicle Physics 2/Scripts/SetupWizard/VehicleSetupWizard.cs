using System.Collections.Generic;
using System.Linq;
using NWH.Common.Cameras;
using NWH.Common.CoM;
using NWH.Common.Input;
using NWH.Common.SceneManagement;
using NWH.Common.Utility;
using NWH.Common.Vehicles;
using NWH.VehiclePhysics2.Input;
using NWH.VehiclePhysics2.Modules.MotorcycleModule;
using NWH.VehiclePhysics2.Powertrain;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif



namespace NWH.VehiclePhysics2.SetupWizard
{
    /// <summary>
    ///     Script used to set up vehicle from a model.
    ///     Can be used through editor or called at run-time.
    ///     Requires model with separate wheels and Unity-correct scale, rotation and pivots.
    /// </summary>
    public partial class VehicleSetupWizard : MonoBehaviour
    {
        public const float INIT_SPRING_LENGTH = 0.2f;

        public enum WheelControllerType
        {
            WheelController3D,
            UnityWheelCollider,
        }

        /// <summary>
        ///     Should a default vehicle camera and camera changer be added?
        /// </summary>
        [Tooltip("    Should a default vehicle camera and camera changer be added?")]
        public bool addCamera = true;


        public WheelControllerType wheelControllerType;

        /// <summary>
        ///     Should character enter/exit points be added?
        /// </summary>
        [Tooltip("    Should character enter/exit points be added?")]
        public bool addCharacterEnterExitPoints = true;

        /// <summary>
        ///     Should the vehicle be added to the vehicles list of the VehicleChanger (if present in the scene)?
        /// </summary>
        [UnityEngine.Tooltip("    Should the vehicle be added to the vehicles list of the VehicleChanger (if present in the scene)?")]
        public bool registerWithVehicleChanger = true;

        /// <summary>
        ///     Wheel GameObjects in order: front-left, front-right, rear-left, rear-right, etc.
        /// </summary>
        [Tooltip("    Wheel GameObjects in order: front-left, front-right, rear-left, rear-right, etc.")]
        public List<GameObject> wheelGameObjects = new List<GameObject>();

        public bool removeWizardOnComplete = true;

        public VehicleSetupWizardPreset preset;

        private GameObject _cameraParent;
        private GameObject _wheelControllerParent;

        // Group parents
        private GameObject _wheelParent;


        /// <summary>
        ///     Sets up a vehicle from scratch. Requires only a model with proper scale, rotation and pivots.
        ///     To adjust the settings to a certain preset, use RunConfiguration afterwards.
        /// </summary>
        /// <param name="targetGO">Root GameObject of the vehicle.</param>
        /// <param name="wheelGOs">Wheel GameObjects in order: front-left, front-right, rear-left, rear-right, etc.</param>
        /// <param name="bodyMeshGO">
        ///     GameObject to which the body MeshCollider will be added. Leave null if it has already been set up.
        ///     It is not recommended to run the setup without any colliders being previously present as this will void inertia and
        ///     center of mass
        ///     calculations during the setup.
        /// </param>
        /// <param name="addCollider">Should MeshCollider be added to bodyMeshGO?</param>
        /// <param name="addCamera">Should a default vehicle camera and camera changer be added?</param>
        /// <param name="addCharacterEnterExitPoints">Should character enter/exit points be added?</param>
        /// <returns>Returns newly created VehicleController if setup is successful or null if not.</returns>
        public static VehicleController RunSetup(GameObject targetGO, List<GameObject> wheelGOs, 
            WheelControllerType wheelControllerType,
            bool addCamera = true, bool addCharacterEnterExitPoints = true, bool registerWithVehicleChanger = true)
        {
            Debug.Log("======== VEHICLE SETUP START ========");

#if UNITY_EDITOR
            Undo.RegisterCompleteObjectUndo(targetGO, "Run Vehicle Setup Wizard");
#endif

            // ***** Get cached values *****
            Transform transform = targetGO.transform;


            // ***** Check for scale issues *****
            if (transform.localScale != Vector3.one)
            {
                Debug.LogError(
                        "Scale of a parent object should be [1,1,1] for Rigidbody and VehicleController to function properly.");

                return null;
            }


            // ***** Set vehicle tag *****
            targetGO.tag = "Vehicle";


            // ***** Add body collider (if needed) *****
            var colliders = targetGO.GetComponentsInChildren<Collider>();
            if (colliders.Length == 0)
            {
                Debug.LogError("No colliders present on the vehicle. Attach at least one collider (BoxCollider, MeshCollider, etc.)!");
                return null;
            }


            // ***** Add rigidbody *****
            Debug.Log($"Adding Rigidbody to {targetGO.name}");

            Rigidbody vehicleRigidbody = targetGO.gameObject.AddComponent<Rigidbody>();
            vehicleRigidbody.mass = 1400f;
            vehicleRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            vehicleRigidbody.ResetCenterOfMass();
            vehicleRigidbody.ResetInertiaTensor();


            // ***** Create WheelController GOs and add WheelControllers *****
            foreach (GameObject wheelObject in wheelGOs)
            {
                string objName = $"{wheelObject.name}_WheelController";

                Debug.Log($"Creating new wheel controller object {objName}");

                GameObject wcGo = new GameObject(objName);
                wcGo.tag = "Wheel";
                wcGo.transform.SetParent(transform);

                // Position the WheelController GO to the same position as the wheel plus the spring approx. length
                wcGo.transform.SetPositionAndRotation(wheelObject.transform.position + transform.up * INIT_SPRING_LENGTH, transform.rotation);

                Debug.Log($"   |-> Adding WheelController to {wcGo.name}");

                // Add WheelController / WheelCollider
                WheelUAPI wheelUAPI;

#if !NWH_WC3D
                WheelColliderUAPI wheelColliderUAPI = wcGo.AddComponent<WheelColliderUAPI>();
                wheelUAPI = wheelColliderUAPI;
#else
                if (wheelControllerType == WheelControllerType.UnityWheelCollider)
                {
                    WheelColliderUAPI wheelColliderUAPI = wcGo.AddComponent<WheelColliderUAPI>();
                    wheelUAPI = wheelColliderUAPI;

                    // WheelCollider specific fixes/settings
                    WheelCollider wheelCollider = wcGo.GetComponent<WheelCollider>();
                    if (wheelCollider != null)
                    {
                        var suspensionSpring = wcGo.GetComponent<WheelCollider>().suspensionSpring;
                        suspensionSpring.targetPosition = 0.3f;
                        wheelCollider.suspensionSpring = suspensionSpring;

                        wheelCollider.wheelDampingRate = 0.1f;
                        wheelCollider.forceAppPointDistance = INIT_SPRING_LENGTH * 2f;
                    }
                }
                else
                {
                    NWH.WheelController3D.WheelController wheelController = wcGo.AddComponent<NWH.WheelController3D.WheelController>();
                    wheelUAPI = wheelController;
                }
#endif

                Debug.Assert(wheelUAPI != null, "WheelUAPI is null. Failed to add a wheel.");

                // Assign visual to WheelController
                wheelUAPI.WheelVisual = wheelObject;

                // Position the wheel visual as a child of the wheel controller/collider
                wheelObject.transform.SetParent(wheelUAPI.transform);
                wheelObject.transform.localPosition = -Vector3.up * INIT_SPRING_LENGTH;
                wheelObject.transform.rotation = wcGo.transform.rotation;

                // Attempt to find radius and width
                MeshRenderer mr = wheelObject.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    float radius = mr.bounds.extents.y;
                    float width = mr.bounds.extents.x * 2f;

                    if (radius < 0.05f || radius > 1f)
                    {
                        Debug.LogWarning(
                                "Detected unusual wheel radius. Adjust WheelController's radius field manually.");
                    }
                    else
                    {
                        Debug.Log($"   |-> Setting radius to {radius}");
                        wheelUAPI.Radius = radius;
                    }


                    if (width < 0.02f || width > 1f || width > radius)
                    {
                        Debug.LogWarning(
                                "Detected unusual wheel width. Adjust WheelController's width field manually.");
                    }
                    else
                    {
                        Debug.Log($"   |-> Setting width to {width}");
                        wheelUAPI.Width = width;
                    }
                }
                else
                {
                    Debug.LogWarning(
                            $"Radius and width could not be auto configured. Wheel {wheelObject.name} does not contain a MeshFilter.");
                }
            }


            // ***** Add VehicleController *****
            VehicleController vehicleController = targetGO.GetComponent<VehicleController>();
            if (vehicleController == null)
            {
                Debug.Log($"Adding VehicleController to {targetGO.name}");

                vehicleController = targetGO.AddComponent<VehicleController>();
                vehicleController.SetDefaults();
            }


            // ***** Add VariableCenterOfMass ****
            VariableCenterOfMass vCoM = targetGO.AddComponent<VariableCenterOfMass>();
            vCoM.useDefaultMass = true;
            vCoM.baseMass = 1400f;
            vCoM.useDefaultInertia = true;
            vCoM.useDefaultCenterOfMass = false;
            vCoM.centerOfMass = vCoM.transform.InverseTransformPoint(CalculateCenterOfMass(vehicleController));


            // ***** Add camera *****
            if (addCamera)
            {
                Debug.Log("Adding Cameras.");

                GameObject camerasContainer = new GameObject("Cameras");
                camerasContainer.transform.SetParent(transform);

                Debug.Log("Adding a camera follow.");

                GameObject cameraGO = new GameObject("Vehicle Camera");
                cameraGO.transform.SetParent(camerasContainer.transform);
                Transform t = vehicleController.transform;
                cameraGO.transform.SetPositionAndRotation(t.position, t.rotation);

                Camera camera = cameraGO.AddComponent<Camera>();
                camera.fieldOfView = 80f;

                cameraGO.AddComponent<AudioListener>();

                CameraMouseDrag cameraMouseDrag = cameraGO.AddComponent<CameraMouseDrag>();
                cameraMouseDrag.target = vehicleController.transform;
                cameraMouseDrag.tag = "MainCamera";

                // Add last so initialization find the camera.
                CameraChanger cameraChanger = camerasContainer.AddComponent<CameraChanger>();
            }


            // ***** Add enter/exit points *****
            if (addCharacterEnterExitPoints)
            {
                Debug.Log("Adding enter/exit points.");

                GameObject leftPoint = new GameObject("LeftEnterExitPoint");
                GameObject rightPoint = new GameObject("RightEnterExitPoint");

                leftPoint.transform.SetParent(transform);
                rightPoint.transform.SetParent(transform);

                leftPoint.transform.position = transform.position - transform.right;
                rightPoint.transform.position = transform.position + transform.right;

                leftPoint.tag = "EnterExitPoint";
                rightPoint.tag = "EnterExitPoint";
            }


            // ***** Register with vehicle changer *****
            if (registerWithVehicleChanger)
            {
                VehicleChanger vehicleChanger = FindObjectOfType<VehicleChanger>();
                if (vehicleChanger != null)
                {
                    if (vehicleChanger.vehicles == null) vehicleChanger.vehicles = new List<Vehicle>();
                    vehicleChanger.vehicles.Add(vehicleController);
                }
            }


            // ***** Validate setup *****
            Debug.Log("Validating setup.");


            // Run Validate() on VehicleController which will report if there are any problems with the setup.
            vehicleController.Validate();


            // Check if there are any InputProviders present.
#if UNITY_EDITOR
            if(FindObjectOfType<VehicleInputProviderBase>() == null)
            {
                Debug.LogWarning("No VehicleInputProviders present in the scene. Make sure to add one or the vehicle will not receive any player input. " +
                    "Add InputSystemVehicleInputProvider for InputSystem (recommended), or InputManagerVehicleInputProvider for InputManager, there are others available too.");
            }

            if (FindObjectOfType<SceneInputProviderBase>() == null)
            {
                Debug.LogWarning("No SceneInputProviders present in the scene. Make sure to add one or camera and other controls will not get any player input." +
                    "Add InputSystemSceneInputProvider for InputSystem (recommended), or InputManagerSceneInputProvider for InputManager, there are others available too.");
            }
#endif

            Debug.Log("Setup done.");

            Debug.Log("======== VEHICLE SETUP END ========");

            return vehicleController;
        }



        /// <summary>
        /// Configures vehicle to the given VehicleSetupWizardSettings.
        /// </summary>
        /// <param name="targetVC">Vehicle to configure.</param>
        /// <param name="preset">Settings with which to configure the vehicle.</param>
        public static void RunConfiguration(VehicleController targetVC, VehicleSetupWizardPreset preset)
        {
            Debug.Log($"=== RUNNING CONFIGURATION ({preset.name}) ===");

            if (preset == null)
            {
                Debug.LogError("Configuration can not be ran with null VehicleSetupWizardPreset.");
                return;
            }

            List<WheelUAPI> wheels = targetVC.GetComponentsInChildren<WheelUAPI>().ToList();
            if (wheels.Count == 0)
            {
                Debug.LogError("Vehicle does not have any wheels. Stopping configuration.");
                return;
            }

            if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.Motorcycle)
            {
                if (targetVC.Wheels.Count != 2)
                {
                    Debug.LogError("Wheel count for a motorcycle needs to be 2, in order: front, rear.");
                    return;
                }
            }

            // Physical properties
            Rigidbody rb = targetVC.GetComponent<Rigidbody>();
            rb.mass = preset.mass;

            VariableCenterOfMass vcom = targetVC.GetComponent<VariableCenterOfMass>();
            vcom.baseMass = preset.mass;

            // State settings
            if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SemiTruck)
            {
                targetVC.stateSettings = Resources.Load("NWH Vehicle Physics 2/Defaults/SemiTruckStateSettings") as StateSettings;
            }
            else if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.Trailer)
            {
                targetVC.stateSettings = Resources.Load("NWH Vehicle Physics 2/Defaults/TrailerStateSettings") as StateSettings;
            }

            // Powertrain
            float inertiaBase;
            switch (preset.vehicleType)
            {
                case VehicleSetupWizardPreset.VehicleType.SemiTruck:
                    inertiaBase = 0.8f;
                    break;
                case VehicleSetupWizardPreset.VehicleType.MonsterTruck:
                    inertiaBase = 0.4f;
                    break;
                case VehicleSetupWizardPreset.VehicleType.OffRoad:
                    inertiaBase = 0.3f;
                    break;
                case VehicleSetupWizardPreset.VehicleType.SportsCar:
                    inertiaBase = 0.18f;
                    break;
                case VehicleSetupWizardPreset.VehicleType.Motorcycle:
                    inertiaBase = 0.13f;
                    break;
                default:
                    inertiaBase = 0.22f;
                    break;
            }

            targetVC.powertrain.engine.inertia = inertiaBase;
            targetVC.powertrain.transmission.inertia = inertiaBase * 0.05f;
            targetVC.powertrain.clutch.inertia = inertiaBase * 0.05f;

            // Engine
            targetVC.powertrain.engine.maxPower = preset.enginePower;
            targetVC.powertrain.engine.maxLossTorque = preset.enginePower * 0.6f;
            targetVC.powertrain.engine.revLimiterRPM = preset.engineMaxRPM * 0.94f;
            targetVC.powertrain.engine.maxRPM = preset.engineMaxRPM;
            targetVC.powertrain.engine.forcedInduction.useForcedInduction =
                preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SportsCar;

            float enginePeakPower = 0;
            float enginePeakPowerRpm = 5000;
            targetVC.powertrain.engine.GetPeakPower(out enginePeakPower, out enginePeakPowerRpm);
            float enginePeakPowerW = UnitConverter.RPMToAngularVelocity(enginePeakPowerRpm);
            float enginePeakTorque = (enginePeakPower * 1000f) / enginePeakPowerW;

            // Clutch
            targetVC.powertrain.clutch.engagementRPM = preset.engineMaxRPM * 0.2f;
            targetVC.powertrain.clutch.engagementRange = preset.engineMaxRPM * 0.04f;
            targetVC.powertrain.engine.UpdatePeakPowerAndTorque();
            targetVC.powertrain.clutch.slipTorque = enginePeakTorque * 1.3f;

            // Transmission
            float shiftDuration = 0.15f;
            targetVC.powertrain.transmission.shiftDuration = shiftDuration;
            targetVC.powertrain.transmission.postShiftBan = 0.3f + shiftDuration;
            targetVC.powertrain.transmission.UpshiftRPM = preset.engineMaxRPM * 0.72f;
            targetVC.powertrain.transmission.DownshiftRPM = preset.engineMaxRPM * 0.2f;

            float finalGearRatio = 4f * (targetVC.Wheels[0].wheelUAPI.Radius / 0.45f);
            finalGearRatio *= preset.transmissionGearing;

            targetVC.powertrain.transmission.finalGearRatio = finalGearRatio;

            TransmissionGearingProfile gearingProfile;
            if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SportsCar)
            {
                gearingProfile = LoadGearingProfile("SportsCar");
            }
            else if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SemiTruck)
            {
                gearingProfile = LoadGearingProfile("SemiTruck");
            }
            else
            {
                gearingProfile = LoadGearingProfile("CompactCar");
            }
            Debug.Assert(gearingProfile != null, "Could not load gearing profile from resources.");

            targetVC.powertrain.transmission.gearingProfile = gearingProfile;

            // Drivetrain
            targetVC.brakes.maxTorque = preset.mass * 1.4f;

            // Assumes diff 0 is front, diff 1 is rear and diff 2 is center - this is the output from the wizard setup.
            if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.Motorcycle)
            {
                targetVC.powertrain.differentials.Clear();
                targetVC.powertrain.transmission.SetOutput(targetVC.Wheels[1]); // Send power to rear wheel directly.
            }
            else
            {
                if (targetVC.Wheels.Count == 4 && targetVC.powertrain.differentials.Count == 3)
                {
                    if (preset.drivetrainConfiguration == VehicleSetupWizardPreset.DrivetrainConfiguration.FWD)
                    {
                        targetVC.powertrain.differentials[2].biasAB = 0f;
                    }
                    else if (preset.drivetrainConfiguration == VehicleSetupWizardPreset.DrivetrainConfiguration.RWD)
                    {
                        targetVC.powertrain.differentials[2].biasAB = 1f;
                    }

                    if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SportsCar)
                    {
                        targetVC.powertrain.differentials[1].differentialType = DifferentialComponent.Type.LimitedSlip;
                    }
                    else if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.OffRoad ||
                             preset.vehicleType == VehicleSetupWizardPreset.VehicleType.MonsterTruck)
                    {
                        targetVC.powertrain.differentials[0].differentialType = DifferentialComponent.Type.Locked;
                        targetVC.powertrain.differentials[1].differentialType = DifferentialComponent.Type.Locked;
                        targetVC.powertrain.differentials[2].differentialType = DifferentialComponent.Type.Locked;
                    }
                }
            }



            // Suspension
            float springLength = 0.35f;
            float springForce = (preset.mass * 50f) / wheels.Count;
            float damperForce = Mathf.Sqrt(springForce) * 12f;
            float slipCoeff = 1f;
            float forceCoeff = 1f;

            springLength *= preset.suspensionTravelCoeff;
            springForce *= preset.suspensionStiffnessCoeff;
            damperForce *= preset.suspensionStiffnessCoeff;

            Debug.Assert(springLength > 0.01f);
            Debug.Assert(springForce > 0f);
            Debug.Assert(damperForce > 0f);
            Debug.Assert(slipCoeff > 0f);
            Debug.Assert(forceCoeff > 0f);

            springLength = Mathf.Clamp(springLength, 0.1f, 1f);

            foreach (WheelUAPI wheelUAPI in wheels)
            {
                wheelUAPI.SpringMaxLength = springLength;
                wheelUAPI.SpringMaxForce = springForce;
                wheelUAPI.DamperMaxReboundForce = damperForce;
                wheelUAPI.DamperMaxBumpForce = damperForce;

                // Calculate wheel mass by assuming heavier vehicle will have heavier wheels.
                wheelUAPI.Mass = Mathf.Clamp(preset.mass / 1500f, 0.2f, 6f) * 20f;

                if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.Motorcycle)
                {
                    wheelUAPI.Width = 0.05f;
                }
            }


            float arb = preset.height * (preset.mass / 20f) * 100f;
            foreach (WheelGroup wheelGroup in targetVC.WheelGroups)
            {
                if (wheelGroup.steerCoefficient == 0) wheelGroup.addAckerman = false;

                if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.Motorcycle)
                {
                    wheelGroup.addAckerman = false;
                    wheelGroup.ToeAngle = 0;
                }
            }

            // Sound
            AudioClip engineRunningClip;
            if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SportsCar)
            {
                engineRunningClip = Resources.Load(GetResourcePath("Sounds/SportsCar")) as AudioClip;
            }
            else if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.MonsterTruck)
            {
                engineRunningClip = Resources.Load(GetResourcePath("Sounds/MuscleCar")) as AudioClip;
            }
            else if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SemiTruck)
            {
                engineRunningClip = Resources.Load(GetResourcePath("Sounds/SemiTruck")) as AudioClip;
            }
            else
            {
                engineRunningClip = Resources.Load(GetResourcePath("Sounds/Car")) as AudioClip;
            }

            targetVC.soundManager.engineRunningComponent.Clip = engineRunningClip;
            targetVC.soundManager.engineRunningComponent.pitchRange = targetVC.powertrain.engine.maxRPM / 2500f;

            // Modules
            if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.Motorcycle)
            {
                targetVC.gameObject.AddComponent<MotorcycleModuleWrapper>();
            }

            // Physics
            if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.Motorcycle)
            {
                targetVC.GetComponent<Rigidbody>().inertiaTensor = new Vector3(50, 90, 35);
            }

            Debug.Log($"Vehicle configured using {preset.name} preset.");
            Debug.Log($"======== VEHICLE CONFIGURATION SUCCESS ========");
        }

        private static string GetResourcePath(string name)
        {
            return $"NWH Vehicle Physics 2/VehicleSetupWizard/{name}";
        }

        private static TransmissionGearingProfile LoadGearingProfile(string name)
        {
            string path = $"NWH Vehicle Physics 2/VehicleSetupWizard/GearingProfile/{name}";
            return Resources.Load(path) as TransmissionGearingProfile;
        }

        /// <summary>
        ///     Calculates a center of mass of the vehicle based on wheel positions.
        ///     Returned value is good enough for general use but manual setting of COM is always recommended if possible.
        /// </summary>
        /// <returns>Center of mass of the vehicle's Rigidbody in world coordinates.</returns>
        private static Vector3 CalculateCenterOfMass(VehicleController vc)
        {
            Vector3 centerOfMass = Vector3.zero;
            Vector3 pointSum = Vector3.zero;
            int count = 0;

            foreach (WheelUAPI wheelUAPI in vc.gameObject.GetComponentsInChildren<WheelUAPI>())
            {
                pointSum += wheelUAPI.transform.position;
                count++;
            }

            if (count > 0)
            {
                centerOfMass = pointSum / count;
            }

            centerOfMass -= INIT_SPRING_LENGTH * 0.15f * vc.transform.up;

            return centerOfMass;
        }
    }
}