using System;
using System.Collections.Generic;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using UnityEngine.Rendering;
using NWH.Common.Vehicles;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Effects
{
    /// <summary>
    ///     Generates skidmark meshes.
    /// </summary>
    public partial class SkidmarkGenerator
    {
        private Bounds _bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        private Color _color = new Color32(0, 0, 0, 0);
        private Color[] _colors;
        private SkidmarkRect _currentRect;
        private Vector3 _direction, _xDirection;
        private float _intensity;
        private float _intensityVelocity;
        private bool _isGrounded;
        private bool _isInitial = true;
        private float _markWidth = -1f;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private float _minSqrDistance;
        private Vector3[] _normals;
        private Color _prevColor = new Color(0, 0, 0, 0);
        private float _prevIntensity;
        private SkidmarkRect _previousRect;
        private int _prevSurfaceMapIndex;
        private int _sectionCount;
        private SkidmarkDestroy _skidmarkDestroy;
        private Mesh _skidmarkMesh;
        private int _surfaceMapIndex = -1;
        private Vector4[] _tangents;
        private WheelComponent _targetWheelComponent;
        private int[] _triangles;
        private Vector2[] _uvs;
        private Vector2 _vector00 = new Vector2(0, 0);
        private Vector2 _vector01 = new Vector2(0, 1);
        private Vector2 _vector10 = new Vector2(1, 0);
        private Vector2 _vector11 = new Vector2(1, 1);
        private int _triCount;
        private Queue<GameObject> _skidObjectQueue = new Queue<GameObject>();
        private GameObject _currentSkidObject;

        private Vector3[] _vertices;
        private bool _wasGroundedFlag;
        private SkidmarkManager _skidmarkManager;


        public bool Initialize(SkidmarkManager skidmarkManager, WheelComponent wheelComponent)
        {
            _skidmarkManager = skidmarkManager;
            _targetWheelComponent = wheelComponent;
            _minSqrDistance = skidmarkManager.minDistance * skidmarkManager.minDistance;
            _triCount = 0;

            _skidObjectQueue = new Queue<GameObject>();
            _markWidth = wheelComponent.wheelUAPI.Width;

            _isInitial = true;
            _prevSurfaceMapIndex = -999;

            GenerateNewSection();
            return true;
        }


        public void Generate(int surfaceMapIndex, float targetIntensity, Vector3 velocity, float dt)
        {
            _isGrounded = _targetWheelComponent.wheelUAPI.IsGrounded;
            _prevIntensity = _intensity;

            _prevSurfaceMapIndex = _surfaceMapIndex;
            _surfaceMapIndex = surfaceMapIndex;

            if (!_isGrounded)
            {
                _intensity = 0;
                _wasGroundedFlag = false;
            }

            if (targetIntensity < _skidmarkManager.lowerIntensityThreshold)
            {
                _intensity = 0;
                _isGrounded = false;
            }
            else
            {
                _intensity = Mathf.SmoothDamp(_intensity, targetIntensity, ref _intensityVelocity,
                    _skidmarkManager.smoothing);
            }

            if (_surfaceMapIndex != _prevSurfaceMapIndex)
            {
                GenerateNewSection();
            }

            // Calculate skidmark intensity on hard surfaces (asphalt, concrete, etc.)
            if (surfaceMapIndex >= 0)
            {
                // Get current position
                Vector3 currentPosition = _targetWheelComponent.wheelUAPI.HitPoint;
                currentPosition += _targetWheelComponent.wheelUAPI.HitNormal * _skidmarkManager.groundOffset;
                currentPosition += velocity * dt * 0.5f;
                currentPosition = _currentSkidObject.transform.InverseTransformPoint(currentPosition);

                // Check distance
                float sqrDistance = (currentPosition - _previousRect.position).sqrMagnitude;
                if (sqrDistance < _minSqrDistance)
                {
                    return;
                }

                // Re-start the skidmark
                if (_isInitial || _isGrounded && !_wasGroundedFlag || _intensity > 0 && _prevIntensity <= 0)
                {
                    Transform controllerTransform = _targetWheelComponent.wheelUAPI.transform;
                    _currentRect.position = currentPosition;
                    _currentRect.normal = _targetWheelComponent.wheelUAPI.HitNormal;
                    Vector3 right = controllerTransform.right;
                    _currentRect.positionLeft =
                        currentPosition - right * (_markWidth * 0.5f);
                    _currentRect.positionRight =
                        currentPosition + right * (_markWidth * 0.5f);

                    _direction = controllerTransform.forward;
                    _xDirection = -right;
                    _currentRect.tangent = new Vector4(_xDirection.x, _xDirection.y, _xDirection.y, 1f);

                    _previousRect = _currentRect;
                    _wasGroundedFlag = true;

                    _isInitial = false;
                }
                // Continue the skidmark
                else
                {
                    _currentRect.position = currentPosition;
                    _currentRect.normal = _targetWheelComponent.wheelUAPI.HitNormal;
                    _direction = _currentRect.position - _previousRect.position;
                    _xDirection = Vector3.Cross(_direction, _targetWheelComponent.wheelUAPI.HitNormal)
                                         .normalized;

                    _color.a = _intensity;
                    _currentRect.positionLeft = currentPosition + _xDirection * (_markWidth * 0.5f);
                    _currentRect.positionRight = currentPosition - _xDirection * (_markWidth * 0.5f);
                    _currentRect.tangent = new Vector4(_xDirection.x, _xDirection.y, _xDirection.z, 1f);
                }

                if (_isGrounded)
                {
                    AppendGeometry();
                }

                _previousRect = _currentRect;
            }
        }


        public void GenerateNewSection()
        {
            // Add skid object
            string surfaceName = _targetWheelComponent.surfacePreset == null
                                     ? "Default"
                                     : _targetWheelComponent.surfacePreset.name;

            WheelUAPI wheelUAPI = _targetWheelComponent.wheelUAPI;
            _currentSkidObject = new GameObject($"SkidMesh_{wheelUAPI.transform.parent.name}_" +
                                         $"{wheelUAPI.transform.name}_{_sectionCount}_" +
                                         $"{surfaceName}");
            _currentSkidObject.transform.parent = _skidmarkManager.skidmarkContainer.transform;
            _currentSkidObject.transform.position = wheelUAPI.WheelPosition - wheelUAPI.transform.up * wheelUAPI.Radius;
            _currentSkidObject.isStatic = true;

            if (_skidmarkDestroy != null)
            {
                _skidmarkDestroy.skidmarkIsBeingUsed = false; // Mark old section as not being used any more.
            }

            // Setup skidmark auto-destroy
            _skidmarkDestroy = _currentSkidObject.AddComponent<SkidmarkDestroy>();
            _skidmarkDestroy.targetTransform = _targetWheelComponent.wheelUAPI.transform;
            _skidmarkDestroy.distanceThreshold = _skidmarkManager.skidmarkDestroyDistance;
            _skidmarkDestroy.timeThreshold = _skidmarkManager.skidmarkDestroyTime;
            _skidmarkDestroy.skidmarkIsBeingUsed = true;

            // Setup mesh renderer
            _meshRenderer = _currentSkidObject.GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
            {
                _meshRenderer = _currentSkidObject.AddComponent<MeshRenderer>();
            }

            if (_targetWheelComponent.surfacePreset != null)
            {
                _meshRenderer.material = _targetWheelComponent.surfacePreset.skidmarkMaterial;
            }
            else
            {
                _meshRenderer.material = _skidmarkManager.fallbackMaterial;
            }

            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            _meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            // Add mesh filter
            _meshFilter = _currentSkidObject.AddComponent<MeshFilter>();

            // Init mesh arrays
            _vertices = new Vector3[_skidmarkManager.maxTrisPerSection * 3];
            _normals = new Vector3[_skidmarkManager.maxTrisPerSection * 3];
            _tangents = new Vector4[_skidmarkManager.maxTrisPerSection * 3];
            _colors = new Color[_skidmarkManager.maxTrisPerSection * 3];
            _uvs = new Vector2[_skidmarkManager.maxTrisPerSection * 3];
            _triangles = new int[_skidmarkManager.maxTrisPerSection * 3];

            // Create new mesh
            _skidmarkMesh = new Mesh();
            float maxExtent = _skidmarkManager.minDistance * _skidmarkManager.maxTrisPerSection * 1.1f;
            Vector3 boundsExtents = new Vector3(maxExtent, maxExtent, maxExtent);
            Bounds bounds = new Bounds(_currentSkidObject.transform.position, boundsExtents);
            _skidmarkMesh.bounds = bounds;
            _skidmarkMesh.MarkDynamic();
            _skidmarkMesh.name = "SkidmarkMesh";
            _meshFilter.mesh = _skidmarkMesh;
            _isInitial = true;
            _sectionCount++;

            // Reset counters
            _triCount = 0;

            _skidObjectQueue.Enqueue(_currentSkidObject);
            int skidObjectCount = _skidObjectQueue.Count;
            if (skidObjectCount > 1 && skidObjectCount * _skidmarkManager.maxTrisPerSection > _skidmarkManager.maxTotalTris)
            {
                GameObject lastSection = _skidObjectQueue.Dequeue();
                if (lastSection != null) // Skidmark could already be destroyed by the time or distance condition.
                {
                    SkidmarkDestroy sd = lastSection.GetComponent<SkidmarkDestroy>();
                    if (sd != null)
                    {
                        sd.destroyFlag = true;
                    }
                }
            }
        }


        public void SubArray(ref int[] data, ref int[] outArray, int index, int length)
        {
            Array.Copy(data, index, outArray, 0, length);
        }


        private void AppendGeometry()
        {
            int vertIndex = _triCount * 2;
            int triIndex = _triCount * 3;

            // Generate geometry.
            _vertices[vertIndex + 0] = _previousRect.positionLeft;
            _vertices[vertIndex + 1] = _previousRect.positionRight;
            _vertices[vertIndex + 2] = _currentRect.positionLeft;
            _vertices[vertIndex + 3] = _currentRect.positionRight;

            _normals[vertIndex + 0] = _previousRect.normal;
            _normals[vertIndex + 1] = _previousRect.normal;
            _normals[vertIndex + 2] = _currentRect.normal;
            _normals[vertIndex + 3] = _currentRect.normal;

            _tangents[vertIndex + 0] = _previousRect.tangent;
            _tangents[vertIndex + 1] = _previousRect.tangent;
            _tangents[vertIndex + 2] = _currentRect.tangent;
            _tangents[vertIndex + 3] = _currentRect.tangent;

            _colors[vertIndex + 0] = _prevColor;
            _colors[vertIndex + 1] = _prevColor;

            _color.a = _intensity;

            _colors[vertIndex + 2] = _color;
            _colors[vertIndex + 3] = _color;

            _prevColor = _color;

            _uvs[vertIndex + 0] = _vector00;
            _uvs[vertIndex + 1] = _vector10;
            _uvs[vertIndex + 2] = _vector01;
            _uvs[vertIndex + 3] = _vector11;

            _triangles[triIndex + 0] = vertIndex + 0;
            _triangles[triIndex + 2] = vertIndex + 1;
            _triangles[triIndex + 1] = vertIndex + 2;

            _triangles[triIndex + 3] = vertIndex + 2;
            _triangles[triIndex + 5] = vertIndex + 1;
            _triangles[triIndex + 4] = vertIndex + 3;

            // Reassign the mesh
            _skidmarkMesh.vertices = _vertices;
            _skidmarkMesh.normals = _normals;
            _skidmarkMesh.tangents = _tangents;
            _skidmarkMesh.triangles = _triangles;

            // Assign to mesh
            _skidmarkMesh.colors = _colors;
            _skidmarkMesh.uv = _uvs;
            _skidmarkMesh.bounds = _bounds;
            _meshFilter.mesh = _skidmarkMesh;

            _triCount += 2; // Two triangles per one square / mark.

            if (_triCount + 2 >= _skidmarkManager.maxTrisPerSection) // Check for triangle overflow
            {
                GenerateNewSection();
            }
        }
    }
}