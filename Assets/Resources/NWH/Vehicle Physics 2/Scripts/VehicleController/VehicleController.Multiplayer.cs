using NWH.Common.Vehicles;
using NWH.VehiclePhysics2.Sound.SoundComponents;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Multiplayer-related VehicleController code.
    /// </summary>
    public partial class VehicleController : Vehicle
    {
        public MultiplayerState GetMultiplayerState()
        {
            if (!_multiplayerState.initialized)
            {
                InitializeMultiplayerState();
            }

            // Physics
            _multiplayerState.velocity = vehicleRigidbody.velocity;
            _multiplayerState.angVelocity = vehicleRigidbody.angularVelocity;

            // Input
            _multiplayerState.throttleInput = input.Throttle;
            _multiplayerState.brakeInput = input.Brakes;
            _multiplayerState.steeringInput = input.Steering;

            // Sound
            for (int i = 0; i < soundManager.Components.Count; i++)
            {
                (soundManager.Components[i] as SoundComponent).GetNetworkState(out bool playing, out float volume, out float pitch);
                _multiplayerState.soundVolumeArray[i] = volume;
                _multiplayerState.soundPitchArray[i] = pitch;
                _multiplayerState.soundIsPlayingArray[i] = playing;
            }

            // Effects
            _multiplayerState.lightState = effectsManager.lightsManager.GetIntState();

            return _multiplayerState;
        }


        public bool SetMultiplayerState(MultiplayerState inboundState)
        {
            if (!_multiplayerState.initialized)
            {
                InitializeMultiplayerState();
            }

            // Physics
            vehicleRigidbody.velocity = inboundState.velocity;
            vehicleRigidbody.angularVelocity = inboundState.angVelocity;

            // Input
            input.Throttle = inboundState.throttleInput;
            input.Brakes = inboundState.brakeInput;
            input.Steering = inboundState.steeringInput;

            // Sound
            for (int i = 0; i < soundManager.Components.Count; i++)
            {
                float volume = inboundState.soundVolumeArray[i];
                float pitch = inboundState.soundPitchArray[i];
                bool isPlaying = inboundState.soundIsPlayingArray[i];
                (soundManager.Components[i] as SoundComponent).SetNetworkState(isPlaying, volume, pitch);
            }

            // Effects
            effectsManager.lightsManager.SetStateFromInt(inboundState.lightState);

            return true;
        }


        private void InitializeMultiplayerState()
        {
            _multiplayerState = new MultiplayerState();
            int soundComponentCount = soundManager.Components.Count;
            _multiplayerState.soundVolumeArray = new float[soundComponentCount];
            _multiplayerState.soundPitchArray = new float[soundComponentCount];
            _multiplayerState.soundIsPlayingArray = new bool[soundComponentCount];
            _multiplayerState.initialized = true;
        }


        public struct MultiplayerState
        {
            public bool initialized;
            public Vector3 velocity;
            public Vector3 angVelocity;
            public float throttleInput;
            public float brakeInput;
            public float steeringInput;
            public float[] soundVolumeArray;
            public float[] soundPitchArray;
            public bool[] soundIsPlayingArray;
            public int lightState;
        }
    }
}
