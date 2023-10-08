using System;
using System.Collections.Generic;
using NWH.Common.Vehicles;
using NWH.VehiclePhysics2.Sound.SoundComponents;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NWH.VehiclePhysics2.Sound
{
    /// <summary>
    ///     Main class that manages all the sound aspects of the vehicle.
    /// </summary>
    [Serializable]
    public partial class SoundManager : ManagerVehicleComponent
    {
        // ***** COMPONENTS *****

        /// <summary>
        ///     Sound of engine idling.
        /// </summary>
        [Tooltip("    Sound of engine idling.")]
        public EngineRunningComponent engineRunningComponent = new EngineRunningComponent();

        /// <summary>
        ///     Engine start / stop component. First clip is for starting and second one is for stopping.
        /// </summary>
        [Tooltip("    Engine start / stop component. First clip is for starting and second one is for stopping.")]
        public EngineStartComponent engineStartComponent = new EngineStartComponent();

        /// <summary>
        /// Sound of the engine cooling fan. Can also be used to add additional sound layers to the engine instead.
        /// </summary>
        [UnityEngine.Tooltip("Sound of the engine cooling fan. Can also be used to add additional sound layers to the engine instead.")]
        public EngineFanComponent engineFanComponent = new EngineFanComponent();

        /// <summary>
        /// Sound of the engine popping on throttle release.
        /// </summary>
        public ExhaustPopComponent exhaustPopComponent = new ExhaustPopComponent();

        /// <summary>
        ///     Sound from changing gears. Supports multiple clips.
        /// </summary>
        [Tooltip("    Sound from changing gears. Supports multiple clips.")]
        public GearChangeComponent gearChangeComponent = new GearChangeComponent();

        /// <summary>
        ///     Transmission whine from straight cut gears or just a noisy gearbox.
        /// </summary>
        [Tooltip("    Transmission whine from straight cut gears or just a noisy gearbox.")]
        public TransmissionWhineComponent transmissionWhineComponent = new TransmissionWhineComponent();

        /// <summary>
        ///     Sound of turbo's wastegate. Supports multiple clips.
        /// </summary>
        [Tooltip("    Sound of turbo's wastegate. Supports multiple clips.")]
        public TurboFlutterComponent turboFlutterComponent = new TurboFlutterComponent();

        /// <summary>
        ///     Forced induction whistle component. Can be used for air intake noise or supercharger if spool up time is set to 0
        ///     under engine settings.
        /// </summary>
        [Tooltip(
            "Forced induction whistle component. Can be used for air intake noise or supercharger if spool up time is set to 0 under engine settings.")]
        public TurboWhistleComponent turboWhistleComponent = new TurboWhistleComponent();

        /// <summary>
        ///     Sound produced by wheel skidding over a surface. Tire squeal.
        /// </summary>
        [Tooltip("    Sound produced by wheel skidding over a surface. Tire squeal.")]
        public WheelSkidComponent wheelSkidComponent = new WheelSkidComponent();

        /// <summary>
        ///     Sound produced by wheel rolling over a surface. Tire hum.
        /// </summary>
        [Tooltip("    Sound produced by wheel rolling over a surface. Tire hum.")]
        public WheelTireNoiseComponent wheelTireNoiseComponent = new WheelTireNoiseComponent();

        /// <summary>
        ///     Sound from wheels hitting ground and/or obstracles. Supports multiple clips.
        /// </summary>
        [Tooltip("    Sound from wheels hitting ground and/or obstracles. Supports multiple clips.")]
        public SuspensionBumpComponent suspensionBumpComponent = new SuspensionBumpComponent();

        /// <summary>
        ///     Tick-tock sound of a working blinker. First clip is played when blinker is turning on and second clip is played
        ///     when blinker is turning off.
        /// </summary>
        [Tooltip(
            "Tick-tock sound of a working blinker. First clip is played when blinker is turning on and second clip is played when blinker is turning off.")]
        public BlinkerComponent blinkerComponent = new BlinkerComponent();

        /// <summary>
        ///     Sound of air brakes releasing air. Supports multiple clips.
        /// </summary>
        [Tooltip("    Sound of air brakes releasing air. Supports multiple clips.")]
        public AirBrakeComponent airBrakeComponent = new AirBrakeComponent();

        /// <summary>
        ///     Sound of vehicle hitting other objects. Supports multiple clips.
        /// </summary>
        [Tooltip("    Sound of vehicle hitting other objects. Supports multiple clips.")]
        public CrashComponent crashComponent = new CrashComponent();

        /// <summary>
        ///     Horn sound.
        /// </summary>
        [UnityEngine.Tooltip("    Horn sound.")]
        public HornComponent hornComponent = new HornComponent();

        /// <summary>
        ///     Reverse beep used as warning on most commercial vehicles.
        /// </summary>
        [UnityEngine.Tooltip("    Reverse beep used as warning on most commercial vehicles.")]
        public ReverseBeepComponent reverseBeepComponent = new ReverseBeepComponent();




        // ***** MIXER GROUPS *****

        /// <summary>
        ///     Optional custom mixer. If left empty default will be used (VehicleAudioMixer in Resources folder).
        /// </summary>
        [Tooltip(
            "    Optional custom mixer. If left empty default will be used (VehicleAudioMixer in Resources folder).")]
        public AudioMixer mixer;

        /// <summary>
        ///     Main mixer group, affecting all other groups.
        /// </summary>
        [UnityEngine.Tooltip("    Main mixer group, affecting all other groups.")]
        public AudioMixerGroup masterGroup;

        /// <summary>
        ///     Mixer group for all engine sounds.
        /// </summary>
        [UnityEngine.Tooltip("    Mixer group for all engine sounds.")]
        public AudioMixerGroup engineMixerGroup;

        /// <summary>
        /// Mixer group for everything related to the transmission.
        /// </summary>
        [UnityEngine.Tooltip("Mixer group for everything related to the transmission.")]
        public AudioMixerGroup transmissionMixerGroup;

        /// <summary>
        ///     Mixer group for misc sounds.
        /// </summary>
        [UnityEngine.Tooltip("    Mixer group for misc sounds.")]
        public AudioMixerGroup otherMixerGroup;

        /// <summary>
        ///     Mixer group for sounds caused by wheel interaction with the surface.
        /// </summary>
        [UnityEngine.Tooltip("    Mixer group for sounds caused by wheel interaction with the surface.")]
        public AudioMixerGroup surfaceNoiseMixerGroup;




        // ***** SOURCE CONTAINERS *****

        /// <summary>
        ///     GameObject containing all the engine audio sources.
        /// </summary>
        [Tooltip("    GameObject containing all the engine audio sources.")]
        public GameObject engineSourceGO;

        /// <summary>
        ///     GameObject containing all the crash audio sources.
        /// </summary>
        [Tooltip("    GameObject containing all the crash audio sources.")]
        public GameObject crashSourceGO;

        /// <summary>
        ///     GameObject containing all other audio sources.
        /// </summary>
        [Tooltip("    GameObject containing all other audio sources.")]
        public GameObject otherSourceGO;

        /// <summary>
        ///     GameObject containing all the exhaust audio sources.
        /// </summary>
        [Tooltip("    GameObject containing all the exhaust audio sources.")]
        public GameObject exhaustSourceGO;

        /// <summary>
        ///     GameObject containing all transmission audio sources.
        /// </summary>
        [Tooltip("    GameObject containing all transmission audio sources.")]
        public GameObject transmissionSourceGO;




        // ***** MASTER SETTINGS *****

        /// <summary>
        ///     Master volume of a vehicle. To adjust volume of all vehicles or their components check audio mixer.
        /// </summary>
        [Range(0, 2)]
        [Tooltip(
            "    Master volume of a vehicle. To adjust volume of all vehicles or their components check audio mixer.")]
        public float masterVolume = 1f;

        /// <summary>
        ///     Sound attenuation inside vehicle.
        /// </summary>
        [Tooltip("    Sound attenuation inside vehicle.")]
        public float interiorAttenuation = -5f;

        /// <summary>
        ///     Frequencies above this frequency will be attenuated.
        /// </summary>
        [UnityEngine.Tooltip("    Frequencies above this frequency will be attenuated.")]
        public float lowPassFrequency = 1700f;

        /// <summary>
        ///     Determines the slope of the low pass filter, i.e. how fast the frequencies will drop off 
        ///     after the low pass frequency.
        /// </summary>
        [Range(0.01f, 10f)]
        [UnityEngine.Tooltip("    Determines the slope of the low pass filter, i.e. how fast the frequencies will drop off \r\n    after the low pass frequency.")]
        public float lowPassQ = 0.5f;

        /// <summary>
        /// Intensity of doppler effect on vehicle audio sources.
        /// </summary>
        [UnityEngine.Tooltip("Intensity of doppler effect on vehicle audio sources.")]
        public float dopplerLevel = 0f;

        /// <summary>
        ///     Spatial blend of all audio sources. Can not be changed at runtime.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Spatial blend of all audio sources. Can not be changed at runtime.")]
        public float spatialBlend = 1.0f;

        private float _originalAttenuation;


        public override List<VehicleComponent> Components
        {
            get
            {
                if (_components == null)
                {
                    _components = new List<VehicleComponent>
                    {
                        engineStartComponent,
                        engineRunningComponent,
                        engineFanComponent,
                        exhaustPopComponent,
                        turboWhistleComponent,
                        turboFlutterComponent,
                        transmissionWhineComponent,
                        gearChangeComponent,
                        airBrakeComponent,
                        blinkerComponent,
                        hornComponent,
                        wheelSkidComponent,
                        wheelTireNoiseComponent,
                        crashComponent,
                        suspensionBumpComponent,
                        reverseBeepComponent,
                    };
                }
                return _components;
            }
        }


        public override void Initialize()
        {
            base.Initialize();

            // Create container game objects for positional audio
            CreateSourceGO("EngineAudioSources", vc.enginePosition, vc.transform, ref engineSourceGO);
            CreateSourceGO("CrashAudioSources", vc.transform.position, vc.transform, ref crashSourceGO);
            CreateSourceGO("TransmissionAudioSources", vc.transmissionPosition, vc.transform, ref transmissionSourceGO);
            CreateSourceGO("ExhaustAudioSources", vc.exhaustPosition, vc.transform, ref exhaustSourceGO);
            CreateSourceGO("OtherAudioSources", new Vector3(0, 0.2f, 0), vc.transform, ref otherSourceGO);

            // Load and setup the mixer
            if (mixer == null)
            {
                mixer = Resources.Load("Sound/VehicleAudioMixer") as AudioMixer;
            }
            Debug.Assert(mixer != null, "Audio mixer is not assigned. Assign it under Sound tab.");

            if (mixer != null)
            {
                try
                {
                    masterGroup = mixer.FindMatchingGroups("Master")[0];
                    engineMixerGroup = mixer.FindMatchingGroups("Engine")[0];
                    transmissionMixerGroup = mixer.FindMatchingGroups("Transmission")[0];
                    surfaceNoiseMixerGroup = mixer.FindMatchingGroups("SurfaceNoise")[0];
                    otherMixerGroup = mixer.FindMatchingGroups("Other")[0];
                }
                catch
                {
                    Debug.LogError("Missing mixer group(s)!");
                }

                mixer.GetFloat("attenuation", out _originalAttenuation);
            }

            // Initialize individual sound components
            foreach (SoundComponent component in Components)
            {
                component.Initialize();
            }


            vc.onCameraEnterVehicle.AddListener(ApplyInsideVehicleMixerSettings);
            vc.onCameraExitVehicle.AddListener(ApplyOutsideVehicleMixerSettings);
            vc.onSleep.AddListener(ApplyOutsideVehicleMixerSettings);
        }


        public override void Start(VehicleController vc)
        {
            base.Start(vc);
        }


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            if (!vc.GetMultiplayerIsRemote())
            {
                base.Update();
            }
        }


        private void ApplyInsideVehicleMixerSettings()
        {
            mixer.SetFloat("attenuation", interiorAttenuation);
            mixer.SetFloat("lowPassFrequency", lowPassFrequency);
            mixer.SetFloat("lowPassQ", lowPassQ);
        }


        private void ApplyOutsideVehicleMixerSettings()
        {
            mixer.SetFloat("attenuation", _originalAttenuation);
            mixer.SetFloat("lowPassFrequency", 22000f);
            mixer.SetFloat("lowPassQ", 1f);
        }


        /// <summary>
        ///     Sets defaults to all the basic sound components when script is first added or reset is called.
        /// </summary>
        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            if (mixer == null)
            {
                mixer = Resources.Load<AudioMixer>(VehicleController.defaultResourcesPath +
                                                   "Sound/VehicleAudioMixer");
                if (mixer == null)
                {
                    Debug.LogWarning("VehicleAudioMixer resource could not be loaded from resources.");
                }
            }
        }

        public void CreateSourceGO(string name, Vector3 localPosition, Transform parent, ref GameObject sourceGO)
        {
            sourceGO = new GameObject();
            sourceGO.name = name;
            sourceGO.transform.SetParent(parent);
            sourceGO.transform.localPosition = localPosition;
        }


        public void RegisterExternalSoundComponent(SoundComponent component)
        {
            component.Start(vc);
            component.Initialize();
            _components.Add(component);
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(SoundManager))]
    public partial class SoundManagerDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.BeginSubsection("Master Settings");
            drawer.Field("masterVolume");
            drawer.Field("spatialBlend");
            drawer.Field("dopplerLevel");
            drawer.Field("mixer");
            drawer.EndSubsection();

            drawer.BeginSubsection("Interior Settings");
            drawer.Field("interiorAttenuation", true, "dB");
            drawer.Field("lowPassFrequency", true, "Hz");
            drawer.Field("lowPassQ");
            drawer.EndSubsection();

            drawer.BeginSubsection("Positional Audio");
            drawer.Info("Go to 'Settings' tab to change component positions.");
            drawer.EndSubsection();

            drawer.BeginSubsection("Components");
            int index = drawer.HorizontalToolbar("soundTab",
                                                 new[]
                                                 {
                                                     "Engine",
                                                     "Forced Ind.",
                                                     "Transmission",
                                                     "Suspension",
                                                     "Ground",
                                                     "Collision",
                                                     "Brakes",
                                                     "Blinkers",
                                                     "Misc",
                                                 });

            switch (index)
            {
                case 0:
                    drawer.Property("engineRunningComponent");
                    drawer.Property("engineStartComponent");
                    drawer.Property("engineFanComponent");
                    drawer.Property("exhaustPopComponent");
                    break;
                case 1:
                    drawer.Property("turboWhistleComponent");
                    drawer.Property("turboFlutterComponent");
                    break;
                case 2:
                    drawer.Property("transmissionWhineComponent");
                    drawer.Property("gearChangeComponent");
                    break;
                case 3:
                    drawer.Property("suspensionBumpComponent");
                    break;
                case 4:
                    drawer.Property("wheelTireNoiseComponent");
                    drawer.Property("wheelSkidComponent");
                    break;
                case 5:
                    drawer.Property("crashComponent");
                    break;
                case 6:
                    drawer.Property("airBrakeComponent");
                    break;
                case 7:
                    drawer.Property("blinkerComponent");
                    break;
                case 8:
                    drawer.Property("hornComponent");
                    drawer.Property("reverseBeepComponent");
                    break;
                default:
                    drawer.Property("engineRunningComponent");
                    break;
            }

            drawer.EndSubsection();
            drawer.EndProperty();
            return true;
        }
    }
}

#endif
