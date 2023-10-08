using System;
using NWH.Common.Vehicles;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif



namespace NWH.VehiclePhysics2.GroundDetection
{
    /// <summary>
    ///     Handles surface/ground detection for the vehicle.
    /// </summary>
    [Serializable]
    public partial class GroundDetection : VehicleComponent
    {
        public GroundDetectionPreset groundDetectionPreset;
        public float groundDetectionInterval = 0.1f;

        private Terrain _activeTerrain;
        private Transform _hitTransform;
        private float[] _mix;
        private float[,,] _splatmapData;
        private TerrainData _terrainData;
        private Vector3 _terrainPos;


        public override void Initialize()
        {
            base.Initialize();

            groundDetectionInterval = UnityEngine.Random.Range(groundDetectionInterval * 0.8f, groundDetectionInterval * 1.2f);
        }

        public override void Enable()
        {
            base.Enable();

            vc.StartCoroutine(GroundDetectionCoroutine());
        }

        public override void Disable()
        {
            base.Disable();

            vc.StopCoroutine(GroundDetectionCoroutine());
        }

        private IEnumerator GroundDetectionCoroutine()
        {
            while (true)
            {
                for (int i = 0; i < vc.Wheels.Count; i++)
                {
                    WheelComponent wheelComponent = vc.Wheels[i];
                    vc.groundDetection.GetCurrentSurfaceMap(wheelComponent.wheelUAPI, ref wheelComponent.surfaceMapIndex, ref wheelComponent.surfacePreset);

                    if (wheelComponent.surfacePreset != null && wheelComponent.surfacePreset.frictionPreset != null)
                    {
                        wheelComponent.wheelUAPI.FrictionPreset = wheelComponent.surfacePreset.frictionPreset;
                    }
                }

                yield return new WaitForSeconds(groundDetectionInterval);
            }
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            if (groundDetectionPreset == null)
            {
                groundDetectionPreset =
                    Resources.Load(VehicleController.defaultResourcesPath + "DefaultGroundDetectionPreset")
                        as GroundDetectionPreset;
            }
        }


        public override void Validate(VehicleController vc)
        {
            base.Validate(vc);

            Debug.Assert(groundDetectionPreset != null, "GroundDetectionPreset is required but is null. " +
                                                        "Go to VehicleController > FX > Grnd. Det. and " +
                                                        "assign a GroundDetectionPreset.");

            if (groundDetectionPreset != null)
            {
                Debug.Assert(groundDetectionPreset.fallbackSurfacePreset != null,
                             "Fallback Surface Preset is not assigned " +
                             $"for {groundDetectionPreset.name}. Fallback Surface Preset is the only required" +
                             " SurfacePreset. Go to VehicleController > FX > Grnd. Det. and " +
                             "assign a Fallback Surface Preset.");

                // Check if surface map tags exist in the scene
                for (int i = 0; i < groundDetectionPreset.surfaceMaps.Count; i++)
                    for (int j = 0; j < groundDetectionPreset.surfaceMaps[i].tags.Count; j++)
                    {
                        string tag = groundDetectionPreset.surfaceMaps[i].tags[j];
                        try
                        {
                            vc.transform.CompareTag(tag);
                        }
                        catch
                        {
                            Debug.LogWarning(
                                $"Tag '{tag}' does not exist in the scene yet the SurfaceMap {groundDetectionPreset.surfaceMaps[i].name}" +
                                " uses it. Make sure to add the missing tag or to remove it from the surface map if not needed." +
                                " This could happen if you are using default/demo GroundDetectionPreset in a project where these tags are not defined.");
                            throw;
                        }
                    }
            }
        }


        /// <summary>
        ///     Gets the surface map the wheel is currently on.
        /// </summary>
        public bool GetCurrentSurfaceMap(WheelUAPI wheelController, ref int surfaceIndex, ref SurfacePreset outSurfacePreset)
        {
            surfaceIndex = -1;
            outSurfacePreset = null;

            if (!IsEnabled)
            {
                return false;
            }

            if (groundDetectionPreset == null)
            {
                Debug.LogError(
                    "GroundDetectionPreset is required but is null. Go to VehicleController > FX > Grnd. Det. and " +
                    "assign a GroundDetectionPreset.");
                return false;
            }

            if (wheelController.HitCollider == null) return false;

            _hitTransform = wheelController.HitCollider.transform;
            if (wheelController.IsGrounded && _hitTransform != null)
            {
                // Check for tags
                int mapCount = groundDetectionPreset.surfaceMaps.Count;
                for (int e = 0; e < mapCount; e++)
                {
                    SurfaceMap map = groundDetectionPreset.surfaceMaps[e];
                    int tagCount = map.tags.Count;

                    for (int i = 0; i < tagCount; i++)
                    {
                        if (_hitTransform.tag == map.tags[i])
                        {
                            outSurfacePreset = map.surfacePreset;
                            surfaceIndex = e;
                            return true;
                        }
                    }
                }

                // Find active terrain
                _activeTerrain = _hitTransform.GetComponent<Terrain>();
                if (_activeTerrain)
                {
                    // Check for terrain textures
                    int dominantTerrainIndex = GetDominantTerrainTexture(wheelController.HitPoint, _activeTerrain);
                    if (dominantTerrainIndex != -1)
                    {
                        for (int e = 0; e < groundDetectionPreset.surfaceMaps.Count; e++)
                        {
                            SurfaceMap map = groundDetectionPreset.surfaceMaps[e];

                            int n = map.terrainTextureIndices.Count;
                            for (int i = 0; i < n; i++)
                            {
                                if (map.terrainTextureIndices[i] == dominantTerrainIndex)
                                {
                                    outSurfacePreset = map.surfacePreset;
                                    surfaceIndex = e;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }


            if (groundDetectionPreset.fallbackSurfacePreset != null)
            {
                outSurfacePreset = groundDetectionPreset.fallbackSurfacePreset;
                surfaceIndex = -1;
                return true;
            }

            Debug.LogError(
                $"Fallback surface map of ground detection preset {groundDetectionPreset.name} not assigned.");
            outSurfacePreset = null;
            surfaceIndex = -1;
            return false;
        }


        /// <summary>
        ///     Returns most prominent texture at the point in a terrain.
        /// </summary>
        public int GetDominantTerrainTexture(Vector3 worldPos, Terrain terrain)
        {
            // returns the zero-based surfaceIndex of the most dominant texture
            // on the main terrain at this world position.
            GetTerrainTextureComposition(worldPos, terrain, ref _mix);
            if (_mix != null)
            {
                float maxMix = 0;
                int maxIndex = 0;
                // loop through each mix value and find the maximum
                for (int n = 0; n < _mix.Length; ++n)
                {
                    if (_mix[n] > maxMix)
                    {
                        maxIndex = n;
                        maxMix = _mix[n];
                    }
                }

                return maxIndex;
            }

            return -1;
        }


        public void GetTerrainTextureComposition(Vector3 worldPos, Terrain terrain, ref float[] cellMix)
        {
            _terrainData = terrain.terrainData;
            _terrainPos = terrain.transform.position;

            int alphamapWidth = _terrainData.alphamapWidth;
            int alphamapHeight = _terrainData.alphamapHeight;

            // Calculate which splat map cell the worldPos falls within (ignoring y)
            int mapX = (int)((worldPos.x - _terrainPos.x) / _terrainData.size.x * alphamapWidth);
            int mapZ = (int)((worldPos.z - _terrainPos.z) / _terrainData.size.z * alphamapHeight);

            mapX = Mathf.Clamp(mapX, 0, alphamapWidth - 1);
            mapZ = Mathf.Clamp(mapZ, 0, alphamapHeight - 1);

            // Get the splat data for this cell as a 1x1xN 3d array (where N = number of textures)
            _splatmapData = _terrainData.GetAlphamaps(mapX, mapZ, 1, 1);
            // Extract the 3D array data to a 1D array:
            cellMix = new float[_splatmapData.GetUpperBound(2) + 1];
            for (int n = 0; n < cellMix.Length; ++n)
            {
                cellMix[n] = _splatmapData[0, 0, n];
            }
        }


        public override void OnDrawGizmosSelected(VehicleController vc)
        {
            base.OnDrawGizmosSelected(vc);

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Handles.color = Color.yellow;
                for (int i = 0; i < vc.Wheels.Count; i++)
                {
                    WheelComponent wheelComponent = vc.Wheels[i];
                    Handles.Label(wheelComponent.wheelUAPI.transform.position, $"  SP: {wheelComponent.surfacePreset?.name}");
                }
            }
#endif
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.GroundDetection
{
    [CustomPropertyDrawer(typeof(GroundDetection))]
    public partial class GroundDetectionDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.BeginSubsection("Debug Info");
            GroundDetection groundDetection =
                SerializedPropertyHelper.GetTargetObjectOfProperty(drawer.serializedProperty) as GroundDetection;
            if (groundDetection != null && groundDetection.VehicleController != null)
            {
                for (int i = 0; i < groundDetection.VehicleController.powertrain.wheels.Count; i++)
                {
                    WheelComponent wheelComponent = groundDetection.VehicleController.powertrain.wheels[i];
                    if (wheelComponent != null)
                    {
                        drawer.Label($"{wheelComponent.name}: {wheelComponent.surfacePreset?.name} SurfacePreset");
                    }
                }
            }
            else
            {
                drawer.Info("Debug info is available only in play mode.");
            }
            drawer.EndSubsection();

            drawer.BeginSubsection("Settings");
            drawer.Field("groundDetectionInterval", true, "s");
            drawer.Field("groundDetectionPreset");

            GroundDetectionPreset gdPreset =
                ((GroundDetection)(SerializedPropertyHelper.GetTargetObjectOfProperty(property)
                                        as VehicleComponent))?.groundDetectionPreset;

            if (gdPreset != null)
            {
                drawer.EmbeddedObjectEditor<NVP_NUIEditor>(gdPreset, drawer.positionRect);
            }
            drawer.EndSubsection();


            drawer.EndProperty();
            return true;
        }
    }
}

#endif
