using NWH.Common.Vehicles;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     LOD-related VehicleController code.
    /// </summary>
    public partial class VehicleController : Vehicle
    {
        /// <summary>
        ///     Distance between camera and vehicle used for determining LOD.
        /// </summary>
        [System.NonSerialized]
        [Tooltip("    Distance between camera and vehicle used for determining LOD.")]
        public float vehicleToCamDistance;

        /// <summary>
        ///     Currently active LOD.
        /// </summary>
        [System.NonSerialized]
        [Tooltip("    Currently active LOD.")]
        public LOD activeLOD;

        /// <summary>
        ///     Currently active LOD index.
        /// </summary>
        [System.NonSerialized]
        [Tooltip("    Currently active LOD index.")]
        public int activeLODIndex;

        /// <summary>
        ///     LODs will only be updated when this value is true.
        ///     Does not affect sleep LOD.
        /// </summary>
        [Tooltip("    LODs will only be updated when this value is true.\r\n    Does not affect sleep LOD.")]
        public bool updateLODs = true;

        /// <summary>
        ///     When enabled Camera.main will be used as lod camera.
        /// </summary>
        [Tooltip("    When enabled Camera.main will be used as lod camera.")]
        public bool useCameraMainForLOD = true;

        /// <summary>
        ///     Camera from which the LOD distance will be measured.
        ///     To use Camera.main instead, set 'useCameraMainForLOD' to true instead.
        /// </summary>
        [Tooltip(
            "Camera from which the LOD distance will be measured.\r\nTo use Camera.main instead, set 'useCameraMainForLOD' to true instead.")]
        public Camera LODCamera;


        /// <summary>
        /// Called when active LOD is changed.
        /// </summary>
        [NonSerialized]
        public UnityEvent onLODChanged = new UnityEvent();


        /// <summary>
        /// Runs the state check on all the components, determining
        /// if they should be enabled or disabled.
        /// </summary>
        public virtual void CheckComponentStates()
        {
            foreach (VehicleComponent component in Components)
            {
                component.CheckState(activeLODIndex);
            }
        }


        /// <summary>
        /// Updates the currently active LOD.
        /// </summary>
        private IEnumerator LODCheckCoroutine()
        {
            while (true)
            {
                LODCheck();

                yield return new WaitForSeconds(UnityEngine.Random.Range(0.4f, 0.5f));
            }
        }


        private void LODCheck()
        {
            if (stateSettings == null)
            {
                return;
            }

            int initLODIndex = activeLODIndex;

            _lodCount = stateSettings.LODs.Count;

            if (!isAwake && _lodCount > 0) // Vehicle is sleeping, force the highest lod
            {
                activeLODIndex = _lodCount - 1;
                activeLOD = stateSettings.LODs[activeLODIndex];
            }
            else if (updateLODs) // Vehicle is awake, determine LOD based on distance
            {
                if (useCameraMainForLOD)
                {
                    LODCamera = Camera.main;
                }
                else
                {
                    if (LODCamera == null)
                    {
                        Debug.LogWarning(
                            "LOD camera is null. Set the LOD camera or enable 'useCameraMainForLOD' instead. Falling back to Camera.main.");
                        LODCamera = Camera.main;
                    }
                }

                // Still null, exit.
                if (LODCamera == null)
                {
                    Debug.LogWarning("LOD camera is null. Make sure that there is a camera with tag 'MainCamera' in the scene and/or that the vehicle cameras have this tag.");
                    return;
                }

                if (_lodCount > 0 && LODCamera != null)
                {
                    _cameraTransform = LODCamera.transform;
                    stateSettings.LODs[_lodCount - 2].distance =
                        Mathf.Infinity; // Make sure last non-sleep LOD is always matched

                    vehicleToCamDistance = Vector3.Distance(vehicleTransform.position, _cameraTransform.position);
                    for (int i = 0; i < _lodCount - 1; i++)
                    {
                        if (stateSettings.LODs[i].distance > vehicleToCamDistance)
                        {
                            activeLODIndex = i;
                            activeLOD = stateSettings.LODs[i];
                            break;
                        }
                    }
                }
                else
                {
                    activeLODIndex = -1;
                    activeLOD = null;
                }
            }

            if (activeLODIndex != initLODIndex)
            {
                onLODChanged.Invoke();
            }
        }
    }
}
