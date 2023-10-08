using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NWH.VehiclePhysics2.Effects;
using NWH.VehiclePhysics2.Input;
using NWH.VehiclePhysics2.Modules;
using NWH.VehiclePhysics2.Powertrain;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using NWH.VehiclePhysics2.Sound;
using NWH.VehiclePhysics2.Sound.SoundComponents;
using NWH.Common.Vehicles;
using System;
using System.Collections;
using NWH.Common.AssetInfo;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Main class controlling all the other parts of the vehicle.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(90)]
    public partial class VehicleController : Vehicle
    {
        public const string defaultResourcesPath = "NWH Vehicle Physics 2/Defaults/";

        public Brakes brakes = new Brakes();
        public DamageHandler damageHandler = new DamageHandler();
        public EffectManager effectsManager = new EffectManager();
        public GroundDetection.GroundDetection groundDetection = new GroundDetection.GroundDetection();
        public VehicleInputHandler input = new VehicleInputHandler();
        public ModuleManager moduleManager = new ModuleManager();
        public Powertrain.Powertrain powertrain = new Powertrain.Powertrain();
        public SoundManager soundManager = new SoundManager();
        public Steering steering = new Steering();

        /// <summary>
        ///     State settings for the current vehicle.
        ///     State settings determine which components are enabled or disabled, as well as which LOD they belong to.
        /// </summary>
        [Tooltip(
            "State settings for the current vehicle.\r\nState settings determine which components are enabled or disabled, as well as which LOD they belong to.")]
        public StateSettings stateSettings;

        /// <summary>
        ///     Used as a threshold value for lateral slip. When absolute lateral slip of a wheel is
        ///     lower than this value wheel is considered to have no lateral slip (wheel skid). Used mostly for effects and sound.
        /// </summary>
        [Tooltip(
            "Used as a threshold value for lateral slip. When absolute lateral slip of a wheel is\r\nlower than this value wheel is considered to have no lateral slip (wheel skid). Used mostly for effects and sound.")]
        public float lateralSlipThreshold = 0.15f;

        /// <summary>
        ///     Used as a threshold value for longitudinal slip. When absolute longitudinal slip of a wheel is
        ///     lower than this value wheel is considered to have no longitudinal slip (wheel spin). Used mostly for effects and
        ///     sound.
        /// </summary>
        [Tooltip(
            "Used as a threshold value for longitudinal slip. When absolute longitudinal slip of a wheel is\r\nlower than this value wheel is considered to have no longitudinal slip (wheel spin). Used mostly for effects and sound.")]
        public float longitudinalSlipThreshold = 0.5f;


        // <summary>
        ///     Position of the engine relative to the vehicle. Turn on gizmos to see the marker.
        /// </summary>
        [Tooltip("    Position of the engine relative to the vehicle. Turn on gizmos to see the marker.")]
        public Vector3 enginePosition = new Vector3(0f, 0.4f, 1.5f);

        /// <summary>
        ///     Position of the exhaust relative to the vehicle. Turn on gizmos to see the marker.
        /// </summary>
        [Tooltip("    Position of the exhaust relative to the vehicle. Turn on gizmos to see the marker.")]
        public Vector3 exhaustPosition = new Vector3(0f, 0.1f, -2f);

        /// <summary>
        ///     Position of the transmission relative to the vehicle. Turn on gizmos to see the marker.
        /// </summary>
        [Tooltip("    Position of the transmission relative to the vehicle. Turn on gizmos to see the marker.")]
        public Vector3 transmissionPosition = new Vector3(0f, 0.2f, 0.2f);

        /// <summary>
        ///     Position of the engine in world coordinates. Used for effects and sound.
        /// </summary>
        public Vector3 WorldEnginePosition
        {
            get { return transform.TransformPoint(enginePosition); }
        }

        /// <summary>
        ///     Position of the exhaust in world coordinates. Used for effects and sound.
        /// </summary>
        public Vector3 WorldExhaustPosition
        {
            get { return transform.TransformPoint(exhaustPosition); }
        }

        /// <summary>
        ///     Position of the transmission in world coordinates. Used for effects and sound.
        /// </summary>
        public Vector3 WorldTransmissionPosition
        {
            get { return transform.TransformPoint(transmissionPosition); }
        }


        /// <summary>
        ///     Valid only for 4-wheeled vehicles with 2 axles (i.e. cars).
        ///     For other vehicles this value will be 0.
        /// </summary>
        [Tooltip(
            "    Valid only for 4-wheeled vehicles with 2 axles (i.e. cars).\r\n    For other vehicles this value will be 0.")]
        public float wheelbase = -1f;

        /// <summary>
        ///     Cached Time.fixedDeltaTime.
        /// </summary>
        [System.NonSerialized]
        [Tooltip("    Cached Time.fixedDeltaTime.")]
        public float fixedDeltaTime = 0.02f;

        /// <summary>
        ///     Cached Time.deltaTime;
        /// </summary>
        [NonSerialized]
        [Tooltip("    Cached Time.deltaTime;")]
        public float deltaTime = 0.02f;

        /// <summary>
        /// Cached version of the Time.realtimeSinceStartup
        /// </summary>
        public float realtimeSinceStartup = 0f;

        /// <summary>
        /// Amount of time before Freeze While Idle freezes the vehicle.
        /// </summary>
        [UnityEngine.Tooltip("Amount of time before Freeze While Idle freezes the vehicle.")]
        public float inactivityTimeout = 1.3f;

        /// <summary>
        ///     Called after vehicle has finished initializing.
        /// </summary>
        [NonSerialized]
        [Tooltip("    Called after vehicle has finished initializing.")]
        public UnityEvent onVehicleInitialized = new UnityEvent();


        [NonSerialized]
        private List<VehicleComponent> _components = null;

        /// <summary>
        /// Struct that holds multiplayer state to be synced over the network.
        /// </summary>
        [NonSerialized]
        protected MultiplayerState _multiplayerState;


        private Transform _cameraTransform;
        private int _lodCount;


        /// <summary>
        ///     Wheel groups (i.e. axles) on this vehicle.
        /// </summary>
        public List<WheelGroup> WheelGroups
        {
            get { return powertrain.wheelGroups; }
            private set { powertrain.wheelGroups = value; }
        }


        /// <summary>
        ///     List of all wheels attached to this vehicle.
        /// </summary>
        public List<WheelComponent> Wheels
        {
            get { return powertrain.wheels; }
            private set { powertrain.wheels = value; }
        }

        /// <summary>
        /// Is a wheel in air?
        /// </summary>
        public bool HasWheelAir { get; private set; }

        /// <summary>
        /// Does any of the wheel have wheel skid?
        /// </summary>
        public bool HasWheelSkid { get; private set; }

        /// <summary>
        /// Does any of the wheels have wheel spin?
        /// </summary>
        public bool HasWheelSpin { get; private set; }


        /// <summary>
        /// All the VehicleComponents present on this vehicle (SoundManager, InputHandler, etc.)
        /// </summary>
        public List<VehicleComponent> Components
        {
            get
            {
                if (_components == null)
                {
                    _components = new List<VehicleComponent>();
                    _components.Add(input);
                    _components.Add(steering);
                    _components.Add(powertrain);
                    _components.Add(soundManager);
                    _components.Add(effectsManager);
                    _components.Add(damageHandler);
                    _components.Add(brakes);
                    _components.Add(groundDetection);
                    _components.Add(moduleManager);
                }
                return _components;
            }
        }

        /// <summary>
        /// Set to true if the vehicle is receiving data over the network and 
        /// not being simulated locally.
        /// </summary>
        public override void SetMultiplayerIsRemote(bool isRemote)
        {
            if (isRemote) input.autoSetInput = false;

            base.SetMultiplayerIsRemote(isRemote);
        }


        public override void Awake()
        {
            isAwake = false; // Set this to false so that Wake() is initially called.

#if NVP2_DEBUG
            onWake.AddListener(() => { Debug.Log($"++ onWake called on {name}"); });
            onSleep.AddListener(() => { Debug.Log($"++ onSleep called on {name}"); });
            OnVehicleInitialized.AddListener(() => { Debug.Log($"++ OnVehicleInitialized called on {name}"); });
#endif

            base.Awake();
        }


        public override void Start()
        {
            Debug.Assert(transform.localScale == Vector3.one, "Vehicle scale is not 1. Vehicle scale should be [1,1,1]. " +
                "To scale the vehicle use the Unity model import settings or 3D software.");

#if NVP2_DEBUG
            Debug.Log($"Awake() [{name}]");
#endif

            // Initialize components
            foreach (VehicleComponent component in Components)
            {
                component.Start(this);
            }

            onWake.AddListener(CheckComponentStates);
            onSleep.AddListener(CheckComponentStates);
            onLODChanged.AddListener(CheckComponentStates);
            onWake.AddListener(LODCheck);

            StartCoroutine(LODCheckCoroutine());
            CheckComponentStates();

            onVehicleInitialized.Invoke();

            // Put to sleep immediately after initializing 
            if (awakeOnStart)
            {
                isAwake = false;
                Wake();
            }
            else
            {
                isAwake = true;
                Sleep();
            }

            base.Start();
        }


        private void OnDestroy()
        {
            StopAllCoroutines();
        }


        public virtual void Update()
        {
            deltaTime = Time.deltaTime;
            realtimeSinceStartup = Time.realtimeSinceStartup;

            if (GetMultiplayerIsRemote())
            {
                damageHandler.Update();
                effectsManager.Update();
                soundManager.Update();
            }
            else
            {
                foreach (VehicleComponent component in Components)
                {
                    component.Update();
                }
            }
        }


        public override void FixedUpdate()
        {
            base.FixedUpdate();

            fixedDeltaTime = Time.fixedDeltaTime;

            // Cache skid, spin and air values
            HasWheelSkid = false;
            HasWheelSpin = false;
            HasWheelAir = false;

            for (int i = 0; i < Wheels.Count; i++)
            {
                WheelComponent wheelComponent = Wheels[i];
                if (wheelComponent.wheelUAPI.IsSkiddingLongitudinally)
                {
                    HasWheelSpin = true;
                }

                if (wheelComponent.wheelUAPI.IsSkiddingLaterally)
                {
                    HasWheelSkid = true;
                }

                if (!wheelComponent.wheelUAPI.IsGrounded)
                {
                    HasWheelAir = true;
                }
            }

            // Run FixedUpdate on components
            if (GetMultiplayerIsRemote())
            {
                steering.FixedUpdate(); // Is client vehicle, update only steering.
            }
            else
            {
                foreach (VehicleComponent component in Components)
                {
                    component.FixedUpdate();
                }
            }
        }


        public override void Sleep()
        {
            if (!isAwake) return;

#if NVP2_DEBUG
            Debug.Log($"Sleep() [{name}]");
#endif
            int lodCount = stateSettings.LODs.Count;
            if (stateSettings != null && lodCount > 0)
            {
                activeLODIndex = lodCount - 1;
                activeLOD = stateSettings.LODs[activeLODIndex];
            }

            base.Sleep();
        }


        public override void Wake()
        {
            if (isAwake) return;

#if NVP2_DEBUG
            Debug.Log($"Wake() [{name}]");
#endif

            base.Wake();
        }


        /// <summary>
        ///     True if all of the wheels are touching the ground.
        /// </summary>
        public bool IsFullyGrounded()
        {
            int wheelCount = Wheels.Count;
            for (int i = 0; i < wheelCount; i++)
            {
                if (!Wheels[i].wheelUAPI.IsGrounded)
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        ///     True if any of the wheels are touching ground.
        /// </summary>
        public bool IsGrounded()
        {
            int wheelCount = Wheels.Count;
            for (int i = 0; i < wheelCount; i++)
            {
                if (Wheels[i].wheelUAPI.IsGrounded)
                {
                    return true;
                }
            }

            return false;
        }


        public virtual void Reset()
        {
            SetDefaults();
        }


        /// <summary>
        ///     Resets the vehicle to default state.
        ///     Sets default values for all fields and assign default objects from resources folder.
        /// </summary>
        public virtual void SetDefaults()
        {
#if NVP2_DEBUG
            Debug.Log($"SetDefaults() [{name}]");
#endif  
            foreach (VehicleComponent component in Components)
            {
                component.SetDefaults(this);
            }

            if (stateSettings == null)
            {
                stateSettings =
                    Resources.Load(defaultResourcesPath + "DefaultStateSettings") as StateSettings;
            }
        }


        /// <summary>
        /// Validates the vehicle setup and outputs any issues as Debug messages.
        /// </summary>
        public void Validate()
        {
#if NVP2_DEBUG
            Debug.Log($"Validate() [{name}]");
#endif
            Debug.Log("____________________________________________");
            Debug.Log($"{gameObject.name}: Validating VehicleController setup ...");

            if (transform.localScale != Vector3.one)
            {
                Debug.LogWarning(
                    "VehicleController Transform scale is other than [1,1,1]. It is recommended to avoid " +
                    " scaling the vehicle parent object" +
                    " and use Scale Factor from Unity model import settings instead.");
            }

            foreach (VehicleComponent component in Components)
            {
                component.Validate(this);
            }

            Debug.Log($"{gameObject.name}: ... Validation finished. If no warnings or errors showed up, the vehicle is good to go.");
        }



        private void OnCollisionEnter(Collision collision)
        {
            damageHandler.HandleCollision(collision);
        }


        public virtual void OnEnable()
        {
#if NVP2_DEBUG
            Debug.Log($"OnEnable() [{name}]");
#endif

            Wake();
        }


        public virtual void OnDisable()
        {
#if NVP2_DEBUG
            Debug.Log($"OnDisable() [{name}]");
#endif

            Sleep();

            StopCoroutine(LODCheckCoroutine());
        }


        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (vehicleRigidbody == null)
            {
                vehicleRigidbody = GetComponent<Rigidbody>();
            }


            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(WorldEnginePosition, 0.04f);
            Handles.Label(WorldEnginePosition, new GUIContent("  Engine"));

            Gizmos.DrawWireSphere(WorldTransmissionPosition, 0.04f);
            Handles.Label(WorldTransmissionPosition, new GUIContent("  Transmission"));

            Gizmos.DrawWireSphere(WorldExhaustPosition, 0.04f);
            Handles.Label(WorldExhaustPosition, new GUIContent("  Exhaust"));

            Gizmos.color = Color.white;

            foreach (VehicleComponent component in Components)
            {
                steering.OnDrawGizmosSelected(this);
                powertrain.OnDrawGizmosSelected(this);
                soundManager.OnDrawGizmosSelected(this);
                effectsManager.OnDrawGizmosSelected(this);
                damageHandler.OnDrawGizmosSelected(this);
                brakes.OnDrawGizmosSelected(this);
                groundDetection.OnDrawGizmosSelected(this);
                moduleManager.OnDrawGizmosSelected(this);
            }
#endif
        }
    }

#if UNITY_EDITOR
    /// <summary>
    ///     Inspector for VehicleController.
    /// </summary>
    [CustomEditor(typeof(VehicleController))]
    [CanEditMultipleObjects]
    public partial class VehicleControllerEditor : NVP_NUIEditor
    {
        private VehicleController vc;


        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            vc = (VehicleController)target;

            Rect awakeButtonRect = new Rect(drawer.positionRect.x + drawer.positionRect.width - 58f,
                                            drawer.positionRect.y - 20f,
                                            56f, 15f);

            // Draw logo texture
            Rect logoRect = drawer.positionRect;
            logoRect.height = 60f;
            drawer.DrawEditorTexture(logoRect, "NWH Vehicle Physics 2/Editor/logo_bg", ScaleMode.ScaleAndCrop);
            drawer.DrawEditorTexture(
                new Rect(logoRect.x + 8f, logoRect.y + 11f, logoRect.width - 8f, logoRect.height - 22f),
                "NWH Vehicle Physics 2/Editor/logo_light", ScaleMode.ScaleToFit);
            drawer.AdvancePosition(logoRect.height);


            // Draw awake button
            Color initGUIColor = GUI.color;
            GUI.color = vc.IsAwake ? NUISettings.enabledColor : NUISettings.disabledColor;
            GUIStyle awakeButtonStyle = new GUIStyle(EditorStyles.miniButton);
            awakeButtonStyle.fixedHeight = 15f;

            if (Application.isPlaying)
            {
                if (GUI.Button(awakeButtonRect, vc.IsAwake ? "AWAKE" : "ASLEEP", awakeButtonStyle))
                {
                    if (vc.IsAwake)
                    {
                        vc.Sleep();
                    }
                    else
                    {
                        vc.Wake();
                    }
                }

                GUI.color = initGUIColor;

                // Draw lod text
                Rect lodRect = awakeButtonRect;
                lodRect.y += 22f;
                GUI.Label(lodRect, "LOD " + vc.activeLODIndex, EditorStyles.whiteMiniLabel);
            }

            Rect stateSettingsRect = awakeButtonRect;
            stateSettingsRect.x -= 140f;
            stateSettingsRect.width = 200f;

            GUIStyle miniStyle = EditorStyles.whiteMiniLabel;
            miniStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Label(stateSettingsRect, vc.stateSettings?.name, miniStyle);

            GUI.color = initGUIColor;


            if (Application.isPlaying)
            {
                drawer.Info("Performance will be somewhat reduced while VehicleController inspector is open due to its complexity.\n" +
                            "When doing any performance testing please collapse or close this inspector window.");
            }

            if (Time.fixedDeltaTime >= 0.02f)
            {
                drawer.Info("Current Project Settings > Time > Fixed Timestep is higher than 0.02.\n " +
                            "For best results use 0.02 (50Hz physics update) or lower for desktop (0.016667, 0.01333 or 0.01).");
            }


            // Draw toolbar
            int categoryTab = drawer.HorizontalToolbar("categoryTab",
                                                        new[]
                                                        {
                                                        "Sound", "FX", "PWR", "Control", "Settings", "About",
                                                        }, true, true);
            drawer.Space(2);

            if (categoryTab == 0) // FX
            {
#if NVP2_FMOD
            drawer.Info("When NVP2_FMOD is defined sound settings are set through Modules > Sound > FMOD Module.", MessageType.Warning);
#else
                drawer.Property("soundManager");
#endif
            }
            else if (categoryTab == 1)
            {
                int fxTab = drawer.HorizontalToolbar("fxTab",
                                                        new[] { "Effects", "Grnd. Det.", "Damage", }, true, true);
                drawer.Space(2);

                if (fxTab == 0) // Effects
                {
                    drawer.Property("effectsManager");
                }
                else if (fxTab == 1)
                {
                    drawer.Property("groundDetection");
                }
                else if (fxTab == 2)
                {
                    drawer.Property("damageHandler");
                }
            }
            else if (categoryTab == 2) // Powertrain
            {
                drawer.Property("powertrain");
            }                          // Powertrain
            else if (categoryTab == 3) // Control
            {
                int controlTab =
                    drawer.HorizontalToolbar("controlTab", new[] { "Input", "Steering", "Brakes", }, true, true);
                switch (controlTab)
                {
                    case 0:
                        DrawInputTab();
                        break;
                    case 1:
                        drawer.Property("steering");
                        break;
                    case 2:
                        drawer.Property("brakes");
                        break;
                }
            }
            else if (categoryTab == 4) // Settings
            {
                DrawSettingsTab();
            }
            else if (categoryTab == 5)
            {
                DrawAboutTab();
            }
            else
            {
                categoryTab = 0;
            }

            if (drawer.totalHeight < 800)
            {
                drawer.totalHeight = 800;
            }

            drawer.EndEditor(this);
            return true;
        }


        public override bool UseDefaultMargins()
        {
            return false;
        }


        private void DrawInputTab()
        {
            drawer.Property("input");
        }


        private void DrawAboutTab()
        {
            AssetInfo assetInfo = Resources.Load("NWH Vehicle Physics 2/NWH Vehicle Physics 2 AssetInfo") as AssetInfo;
            if (assetInfo == null)
            {
                return;
            }

            GUILayout.Space(drawer.positionRect.y - 20f);
            WelcomeMessageWindow.DrawWelcomeMessage(assetInfo, EditorGUIUtility.currentViewWidth);
        }

        private void DrawSettingsTab()
        {
            drawer.Header("Settings");

            drawer.BeginSubsection("General");
            {
                drawer.Field("awakeOnStart");
                drawer.Field("registerWithVehicleChanger");
            }
            drawer.EndSubsection();


            drawer.BeginSubsection("Actions");
            if (drawer.Button("Validate Setup"))
            {
                vc.Validate();
            }
            drawer.EndSubsection();

            drawer.BeginSubsection("State Settings");

            drawer.Field("stateSettings");
            if (vc.stateSettings == null)
            {
                drawer.Info("StateSettings not assigned. To use component states and LODs assign StateSettings.",
                            MessageType.Warning);
            }
            drawer.EndSubsection();


            drawer.BeginSubsection("LODs");
            drawer.Info("Individual LOD settings can be changed through StateSettings above.");
            drawer.Field("updateLODs", true, null, "Generate LODs");
            if (!drawer.Field("useCameraMainForLOD", true, null, "Use Camera.main For LOD").boolValue)
            {
                drawer.Field("LODCamera");
            }

            if (Application.isPlaying)
            {
                drawer.Label($"Distance To Camera: {vc.vehicleToCamDistance}m");
                string lodName = vc.activeLOD != null ? vc.activeLOD.name : "[not set]";
                drawer.Label($"Active LOD: {vc.activeLODIndex} ({lodName})");
            }
            else
            {
                drawer.Info("Enter play mode to view LOD debug data.");
            }
            drawer.EndSubsection();

            drawer.BeginSubsection("Positions");
            drawer.Field("enginePosition");
            drawer.Field("transmissionPosition");
            drawer.Field("exhaustPosition");
            drawer.EndSubsection();

            drawer.BeginSubsection("Friction");
            drawer.Info("Slip threshold values are used only for effects and sound and do not affect handling.");
            drawer.Field("longitudinalSlipThreshold");
            drawer.Field("lateralSlipThreshold");
            drawer.EndSubsection();

            drawer.BeginSubsection("Debug");
            if (Application.isPlaying)
            {
                drawer.Label($"Is Awake: {vc.IsAwake}");
                drawer.Label($"Current LOD: {vc.activeLOD}");
                drawer.Label($"Multiplayer Is Remote: {vc.GetMultiplayerIsRemote()}");
            }
            else
            {
                drawer.Label("Debug data is visible only in play mode.");
            }
            drawer.EndSubsection();

            drawer.Space(50f);
        }
    }
#endif
}