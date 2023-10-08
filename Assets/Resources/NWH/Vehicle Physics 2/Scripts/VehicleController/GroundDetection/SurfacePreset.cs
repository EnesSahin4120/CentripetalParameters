using System;
using NWH.Common.Vehicles;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.GroundDetection
{
    /// <summary>
    ///     A ScriptableObject that is a collection of multiple GroundDetectionPresets and represents all the ground types in
    ///     the
    ///     scene/project
    ///     that the vehicle can switch between.
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "NWH Vehicle Physics 2", menuName = "NWH/Vehicle Physics 2/Surface Preset", order = 1)]
    public partial class SurfacePreset : ScriptableObject
    {
        public enum ParticleType
        {
            Smoke,
            Dust,
        }

        /// <summary>
        ///     Type of particles generated.
        ///     - Smoke - depends on wheel slip. Use for hard surfaces. Will not emit when there is no wheel slip.
        ///     - Dust - depends on speed only. Use for dusty surfaces, e.g. gravel or sand.
        /// </summary>
        [Tooltip(
            "Type of particles generated.\r\n- Smoke - depends on wheel slip. Use for hard surfaces. Will not emit when there is no wheel slip.\r\n- Dust - depends on speed only. Use for dusty surfaces, e.g. gravel or sand.")]
        public ParticleType particleType = ParticleType.Smoke;

        /// <summary>
        ///     Color of generated particles on this surface type.
        /// </summary>
        /// SurfacePreset
        [FormerlySerializedAs("dustColor")]
        [Tooltip("    Color of generated particles on this surface type.")]
        public Color particleColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);

        /// <summary>
        ///     Maximum amount of particles emitted over distance.
        /// </summary>
        [Range(0, 2)]
        [Tooltip("    Maximum amount of particles emitted over distance.")]
        public float maxParticleEmissionRateOverDistance = 0.36f;

        /// <summary>
        ///     Initial size of the emitted particles.
        /// </summary>
        [Tooltip("    Initial size of the emitted particles.")]
        public float particleSize = 1f;

        /// <summary>
        ///     Should the particles be emitted on this surface type?
        /// </summary>
        [Tooltip("    Should the particles be emitted on this surface type?")]
        public bool emitParticles = true;

        /// <summary>
        ///     Should dirt chunks / stones be thrown behind the wheel on this surface type?
        /// </summary>
        [Tooltip("    Should dirt chunks / stones be thrown behind the wheel on this surface type?")]
        public bool emitChunks;

        /// <summary>
        ///     Maximum amount of chunks emitted over distance.
        /// </summary>
        [Tooltip("    Maximum amount of chunks emitted over distance.")]
        public float maxChunkEmissionRateOverDistance = 1f;

        /// <summary>
        ///     Determines maximum distance from the wheel that the chunk can stay alive.
        /// </summary>
        [Tooltip("    Determines maximum distance from the wheel that the chunk can stay alive.")]
        public float chunkLifeDistance = 3f;

        /// <summary>
        ///     Maximum life time of an emitted chunk.
        /// </summary>
        [FormerlySerializedAs("maxChunkLifeTime")]
        [Tooltip("    Maximum life time of an emitted chunk.")]
        public float maxChunkLifetime = 0.5f;

        /// <summary>
        ///     Maximum alpha value start color of an emitted particle can achieve.
        /// </summary>
        [Range(0f, 1f)]
        [Tooltip("    Maximum alpha value start color of an emitted particle can achieve.")]
        public float particleMaxAlpha = 0.8f;

        /// <summary>
        ///     Maximum particle start lifetime.
        /// </summary>
        [Tooltip("    Maximum particle start lifetime.")]
        public float maxParticleLifetime = 3.5f;

        /// <summary>
        ///     Maximum distance from the vehicle a particle can achieve.
        /// </summary>
        [Tooltip("    Maximum distance from the vehicle a particle can achieve.")]
        public float particleLifeDistance = 10f;

        /// <summary>
        ///     Friction preset of WC3D that will be used for this surface. More presets can be added in
        ///     WheelController.FrictionPresets.
        /// </summary>
        [Tooltip(
            "Friction preset of WC3D that will be used for this surface. More presets can be added in WheelController.FrictionPresets.")]
        public FrictionPreset frictionPreset;

        /// <summary>
        ///     Name of the surface map.
        /// </summary>
        [Tooltip("Name of the surface map.")]
        public new string name;

        /// <summary>
        ///     AudioClip used for wheel skidding sound effect.
        /// </summary>
        [Tooltip("    AudioClip used for wheel skidding sound effect.")]
        public AudioClip skidSoundClip;

        /// <summary>
        ///     Should tire skid sounds be played for this surface type?
        /// </summary>
        [Tooltip("    Should tire skid sounds be played for this surface type?")]
        public bool playSkidSounds = true;

        /// <summary>
        ///     Sound pitch of wheel skidding over the surface.
        /// </summary>
        [Tooltip("    Sound pitch of wheel skidding over the surface.")]
        public float skidSoundPitch = 1f;

        /// <summary>
        ///     Sound volume of wheel skidding over the surface.
        /// </summary>
        [Tooltip("    Sound volume of wheel skidding over the surface.")]
        public float skidSoundVolume = 0.3f;

        [Range(0, 1)]
        public float slipFactor = 0.5f;

        /// <summary>
        ///     If set to true surface sound volume will be dependent on slip (asphalt, concrete, etc.).
        ///     Set to false for dirt, grass and other soft surfaces.
        /// </summary>
        [FormerlySerializedAs("slipSensitiveSound")]
        [Tooltip("If set to true surface volume will be dependent on slip (asphalt, concrete, etc.)." +
                 " Set to false for dirt, grass and other soft surfaces.")]
        public bool slipSensitiveSurfaceSound;

        /// <summary>
        ///     Should tire rolling over the surface sound be played for this surface type?
        /// </summary>
        [Tooltip("    Should tire rolling over the surface sound be played for this surface type?")]
        public bool playSurfaceSounds = true;

        /// <summary>
        ///     AudioClip used for wheel rolling sound effect.
        /// </summary>
        [Tooltip("    AudioClip used for wheel rolling sound effect.")]
        public AudioClip surfaceSoundClip;

        /// <summary>
        ///     Sound pitch of wheel rolling over the surface.
        /// </summary>
        [Tooltip("    Sound pitch of wheel rolling over the surface.")]
        public float surfaceSoundPitch = 1f;

        /// <summary>
        ///     Sound volume of wheel rolling over the surface.
        /// </summary>
        [Tooltip("    Sound volume of wheel rolling over the surface.")]
        public float surfaceSoundVolume = 0.3f;

        /// <summary>
        ///     Should skid/thread marks be drawn on this surface?
        /// </summary>
        [Tooltip("    Should skid/thread marks be drawn on this surface?")]
        public bool drawSkidmarks = true;

        /// <summary>
        ///     Material used for skid/thread marks on this type of surface.
        /// </summary>
        [Tooltip("    Material used for skid/thread marks on this type of surface.")]
        public Material skidmarkMaterial;

        /// <summary>
        ///     Intensity of the skidmarks when there is no wheel slip.
        ///     Set to 0 for hard surfaces and >0 for soft surfaces where the tire leaves the mark by rolling over it.
        /// </summary>
        [FormerlySerializedAs("baseIntensity")]
        [Range(0, 1)]
        [Tooltip(
            "Intensity of the skidmarks when there is no wheel slip.\r\nSet to 0 for hard surfaces and >0 for soft surfaces where the tire leaves the mark by rolling over it.")]
        public float skidmarkBaseIntensity = 0.5f;
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.GroundDetection
{
    /// <summary>
    ///     Editor for SurfacePreset.
    /// </summary>
    [CustomEditor(typeof(SurfacePreset))]
    [CanEditMultipleObjects]
    public partial class SurfacePresetEditor : NVP_NUIEditor
    {
        private SurfacePreset _surfacePreset;


        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            _surfacePreset = (SurfacePreset)target;

            drawer.BeginSubsection("General");
            drawer.Field("name");
            drawer.EndSubsection();

            drawer.BeginSubsection("Friction Settings");
            drawer.Field("frictionPreset");
            drawer.EndSubsection();

            drawer.BeginSubsection("Skidmark Settings");
            if (drawer.Field("drawSkidmarks").boolValue)
            {
                drawer.Field("skidmarkMaterial");
                drawer.Field("slipFactor");
            }

            drawer.EndSubsection();

            drawer.BeginSubsection("Dust/Smoke Particle Settings");
            if (drawer.Field("emitParticles").boolValue)
            {
                drawer.Field("particleType");
                drawer.Field("particleSize");
                drawer.Field("particleColor");
                drawer.Field("particleMaxAlpha");
                drawer.Field("maxParticleEmissionRateOverDistance");
                drawer.Field("particleLifeDistance");
                drawer.Field("maxParticleLifetime");
            }

            drawer.EndSubsection();

            drawer.BeginSubsection("Chunk Particle Settings");
            if (drawer.Field("emitChunks").boolValue)
            {
                drawer.Field("maxChunkEmissionRateOverDistance");
                drawer.Field("chunkLifeDistance");
                drawer.Field("maxChunkLifetime");
            }

            drawer.EndSubsection();

            drawer.BeginSubsection("Sound Settings");
            drawer.BeginSubsection("Skid Sounds");
            if (drawer.Field("playSkidSounds").boolValue)
            {
                drawer.Field("skidSoundVolume");
                drawer.Field("skidSoundPitch");
                drawer.Field("skidSoundClip");
            }

            drawer.EndSubsection();
            drawer.BeginSubsection("Surface Sounds");
            if (drawer.Field("playSurfaceSounds").boolValue)
            {
                drawer.Field("slipSensitiveSurfaceSound");
                drawer.Field("surfaceSoundVolume");
                drawer.Field("surfaceSoundPitch");
                drawer.Field("surfaceSoundClip");
            }

            drawer.EndSubsection();
            drawer.EndSubsection();

            drawer.EndEditor(this);
            return true;
        }
    }
}

#endif
