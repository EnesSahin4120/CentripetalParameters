using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Base class for all vehicle SoundComponents.
    ///     Inserts a layer above Unity's AudioSource(s) which insures that the values are set properly, master volume is used,
    ///     etc.
    ///     Supports multiple AudioSources/AudioClips per one SoundComponent for random clip switching.
    /// </summary>
    [Serializable]
    public abstract class SoundComponent : VehicleComponent
    {
        /// <summary>
        ///     Base volume of the sound component.
        /// </summary>
        [FormerlySerializedAs("volume")]
        [Range(0f, 1f)]
        [Tooltip("    Base volume of the sound component.")]
        public float baseVolume = 0.1f;

        /// <summary>
        ///     List of audio clips this component can use. Some components can use multiple clips in which case they will be
        ///     chosen at random, and some components can use only one
        ///     in which case only the first clip will be selected. Check manual for more details.
        /// </summary>
        [Tooltip(
            "List of audio clips this component can use. Some components can use multiple clips in which case they will be chosen at random, and some components can use only one " +
            "in which case only the first clip will be selected. Check manual for more details.")]
        public List<AudioClip> clips = new List<AudioClip>();

        /// <summary>
        /// Audio sources for this component. Can be multiple (e.g. multiple wheels per SkidComponent)
        /// </summary>
        [Tooltip("Audio sources for this component. Can be multiple (e.g. multiple wheels per SkidComponent)")]
        [NonSerialized]
        public List<AudioSource> sources = new List<AudioSource>();

        public bool IsPlaying
        {
            get
            {
                return sources.Any(s => s.isPlaying);
            }
        }


        public abstract GameObject ContainerGO { get; }

        public abstract AudioMixerGroup AudioMixerGroup { get; }


        /// <summary>
        ///     Gets or sets the first clip in the clips list.
        /// </summary>
        public AudioClip Clip
        {
            get { return clips.Count > 0 ? clips[0] : null; }
            set
            {
                if (clips.Count > 0)
                {
                    clips[0] = value;
                }
                else
                {
                    clips.Add(value);
                }
            }
        }

        public override void Initialize()
        {
            // Call VehicleComponent Initialize
            base.Initialize();

            CreateAndRegisterAudioSource(AudioMixerGroup, ContainerGO);
        }


        protected AudioSource CreateAndRegisterAudioSource(AudioMixerGroup mixerGroup, GameObject container)
        {
            if (mixerGroup == null)
            {
                Debug.LogError("Trying to use a null mixer group.");
                return null;
            }

            if (container == null)
            {
                Debug.LogError("Trying to use a null container.");
                return null;
            }

            AudioSource source = container.AddComponent<AudioSource>();
            if (source == null)
            {
                Debug.LogError("Failed to create AudioSource.");
                return null;
            }

            source.outputAudioMixerGroup = mixerGroup;
            source.spatialBlend = InitSpatialBlend;
            source.playOnAwake = InitPlayOnAwake;
            source.loop = InitLoop;
            source.volume = InitVolume * vc.soundManager.masterVolume;
            source.clip = InitClip;
            source.priority = 100;
            source.dopplerLevel = InitDopplerLevel;

            int index = sources.Count;
            sources.Add(source);

            if (InitPlayOnAwake)
            {
                Play(index);
            }
            else
            {
                Stop(index);
            }

            sources.Add(source);
            return source;
        }

        /// <summary>
        /// Override to set the initial source loop value.
        /// </summary>
        public virtual bool InitLoop => false;

        /// <summary>
        /// Override to set the initial AudioClip value.
        /// </summary>
        public virtual AudioClip InitClip => Clip;

        /// <summary>
        /// Override to set the initial source volume.
        /// </summary>
        public virtual float InitVolume => baseVolume;

        /// <summary>
        /// Override to set the initial spatial blend.
        /// </summary>
        public virtual float InitSpatialBlend => vc.soundManager.spatialBlend;

        /// <summary>
        /// Override to set the initial doppler level.
        /// </summary>
        public virtual float InitDopplerLevel => vc.soundManager.dopplerLevel;

        /// <summary>
        /// Override to set the initial source play on awake setting.
        /// </summary>
        public virtual bool InitPlayOnAwake => false;

        /// <summary>
        ///     Gets or sets the whole clip list.
        /// </summary>
        public List<AudioClip> Clips
        {
            get { return clips; }
            set { clips = value; }
        }

        /// <summary>
        ///     Gets a random clip from clips list.
        /// </summary>
        public AudioClip RandomClip
        {
            get { return clips[Random.Range(0, clips.Count)]; }
        }


        /// <summary>
        ///     Gets or sets the first audio source in the sources list.
        /// </summary>
        public AudioSource Source
        {
            get
            {
                if (sources.Count > 0)
                {
                    return sources[0];
                }

                return null;
            }
            set
            {
                if (sources.Count > 0)
                {
                    sources[0] = value;
                }

                sources.Add(value);
            }
        }

        /// <summary>
        ///     AudioSources belonging to this SoundComponent.
        /// </summary>
        public List<AudioSource> Sources
        {
            get { return sources; }
            set { sources = value; }
        }


        /// <summary>
        ///     Enables all the AudioSources belonging to this SoundComponent.
        ///     Calls Play() on all the looping sources.
        /// </summary>
        public override void Enable()
        {
            base.Enable();

            foreach (AudioSource source in sources)
            {
                if (!source.enabled)
                {
                    source.enabled = true;
                    if (source.loop)
                    {
                        Play();
                    }
                }
            }
        }


        /// <summary>
        ///     Disables all the AudioSources belonging to this SoundComponent.
        ///     Will call StopEngine() as well as disable the source.
        /// </summary>
        public override void Disable()
        {
            base.Disable();

            foreach (AudioSource source in sources)
            {
                if (source.isPlaying)
                {
                    Stop();
                }

                if (source.enabled)
                {
                    source.enabled = false;
                }

                source.volume = 0;
            }
        }


        /// <summary>
        ///     Gets pitch of the Source. Equal to Source.pitch.
        /// </summary>
        public virtual float GetPitch()
        {
            if (!Active)
            {
                return 0;
            }

            return Source.pitch;
        }


        /// <summary>
        ///     Gets volume of the Source. Equal to Source.volume.
        /// </summary>
        public virtual float GetVolume()
        {
            if (!Active)
            {
                return 0;
            }

            return Source.volume;
        }

        public virtual void PlayRandomClip()
        {
            Play(0, Random.Range(0, clips.Count));
        }

        /// <summary>
        ///     Plays the source at index if not already playing.
        /// </summary>
        /// <param name="sourceIndex">Index of the source to play.</param>
        public virtual void Play(int sourceIndex = 0, int clipIndex = 0)
        {
            if (!Active)
            {
                return;
            }

            if (Sources.Count <= sourceIndex)
            {
                Debug.LogWarning("Source index out of range.");
                return;
            }

            AudioSource s = Sources[sourceIndex];

            if (s == null)
            {
                Debug.LogWarning($"AudioSource does not exist.");
                return;
            }

            if (Clips.Count > clipIndex)
            {
                s.clip = Clips[clipIndex];
            }
            else
            {
                return; // Sometimes no clip is normal as e.g. surface has not yet been set.
                // Debug.LogWarning($"{GetType().Name}: Trying to play clip at index that does not exist. Ignored." +
                //                  $"Source: {sourceIndex}, Clip: {clipIndex}, Sources: {sources.Count}, Clips: {clips.Count}.");
            }

            s.Play();
        }


        /// <summary>
        ///     Sets pitch for the [id]th source in sources list.
        /// </summary>
        /// <param name="pitch">Pitch to set.</param>
        /// <param name="index">Index of the source.</param>
        public virtual void SetPitch(float pitch, int index)
        {
            if (!Active)
            {
                return;
            }

            pitch = pitch < 0 ? 0 : pitch > 5 ? 5 : pitch;
            sources[index].pitch = pitch;
        }


        /// <summary>
        ///     Sets pitch for the first source in sources list.
        /// </summary>
        /// <param name="pitch">Pitch to set.</param>
        public virtual void SetPitch(float pitch)
        {
            if (!Active)
            {
                return;
            }

            pitch = pitch < 0 ? 0 : pitch > 5 ? 5 : pitch;
            Source.pitch = pitch;
        }


        /// <summary>
        ///     Sets volume for the [id]th source in sources list. Use instead of directly changing source volume as this takes
        ///     master volume into account.
        /// </summary>
        /// <param name="volume">Volume to set.</param>
        /// <param name="index">Index of the target source.</param>
        public virtual void SetVolume(float volume, int index, bool applyMasterVolume = true)
        {
            if (!Active)
            {
                return;
            }

            volume = volume < 0 ? 0 : volume > 2 ? 2 : volume;
            sources[index].volume = volume * (applyMasterVolume ? vc.soundManager.masterVolume : 1f);
        }


        /// <summary>
        ///     Sets the volume of AudioSource. Takes master volume into account.
        /// </summary>
        /// <param name="volume">Volume to set.</param>
        /// <param name="source">Target AudioSource.</param>
        public virtual void SetVolume(float volume, AudioSource source, bool applyMasterVolume = true)
        {
            if (!Active)
            {
                return;
            }

            volume = volume < 0 ? 0 : volume > 2 ? 2 : volume;
            source.volume = volume * (applyMasterVolume ? vc.soundManager.masterVolume : 1f);
        }


        /// <summary>
        ///     Sets volume for the first source in sources list. Use instead of directly changing source volume as this takes
        ///     master volume into account.
        /// </summary>
        /// <param name="volume">Volume to set.</param>
        public virtual void SetVolume(float volume, bool applyMasterVolume = true)
        {
            if (!Active)
            {
                return;
            }

            Source.volume = volume * (applyMasterVolume ? vc.soundManager.masterVolume : 1f);
        }

        /// <summary>
        ///     Stops the AudioSource at index if already playing.
        /// </summary>
        /// <param name="index">Target AudioSource index.</param>
        public virtual void Stop(int index = 0)
        {
            AudioSource s = Sources[index];

            if (!s.isPlaying)
            {
                return;
            }

            s.Stop();
        }


        public virtual void AddDefaultClip(string clipName)
        {
            Clip = Resources.Load(VehicleController.defaultResourcesPath + "Sound/" + clipName) as AudioClip;
            if (Clip == null)
            {
                Debug.LogWarning(
                    $"Audio Clip for sound component {GetType().Name} could not be loaded from resources. " +
                    $"Source will not play." +
                    $"Assign an AudioClip manually.");
            }
        }


        /// <summary>
        /// Gets the SoundComponent network state. Minimum required info to transfer the sound settings over the network.
        /// </summary>
        public virtual void GetNetworkState(out bool isPlaying, out float volume, out float pitch)
        {
            if (Source == null)
            {
                isPlaying = false;
                volume = 0;
                pitch = 0;
                return;
            }

            isPlaying = Source.isPlaying;
            volume = GetVolume();
            pitch = GetPitch();
        }


        /// <summary>
        /// Sets the state to network values. To be used together with GetNetworkState().
        /// </summary>
        public virtual void SetNetworkState(bool isPlaying, float volume, float pitch)
        {
            if (Source == null) return;

            if (isPlaying && !Source.isPlaying)
            {
                Play();
            }
            else if (!isPlaying && Source.isPlaying)
            {
                Stop();
            }

            if (isPlaying)
            {
                SetVolume(volume, false);
                SetPitch(pitch);
            }

        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [CustomPropertyDrawer(typeof(SoundComponent), true)]
    public partial class SoundComponentDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("baseVolume");
            drawer.ReorderableList("clips");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
