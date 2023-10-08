using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Effects
{
    /// <summary>
    ///     Destroys skidmark object when distance to the vehicle becomes greater then distance threshold.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public partial class SkidmarkDestroy : MonoBehaviour
    {
        /// <summary>
        ///     Distance at which the GameObject will be destroyed.
        /// </summary>
        [Tooltip("    Distance at which the GameObject will be destroyed.")]
        public float distanceThreshold = 100f;

        /// <summary>
        /// Time after which the GameObject will be destroyed.
        /// </summary>
        [UnityEngine.Tooltip("Time after which the GameObject will be destroyed.")]
        public float timeThreshold = 20f;

        /// <summary>
        /// True if the skidmark is still the currently active skidmark
        /// </summary>
        [UnityEngine.Tooltip("True if the skidmark is still the currently active skidmark")]
        public bool skidmarkIsBeingUsed;

        /// <summary>
        ///     Transform to which the object belongs to.
        /// </summary>
        [Tooltip("    Transform to which the object belongs to.")]
        public Transform targetTransform;

        /// <summary>
        /// Set to true to trigger the fade out and destroy even with the next check.
        /// </summary>
        [UnityEngine.Tooltip("Set to true to trigger the fade out and destroy even with the next check.")]
        public bool destroyFlag = false;

        private float _fadeOutTimer;
        private float _fadeOutDuration = 5f;
        private MeshRenderer _meshRenderer;
        private float _initMatAlpha;
        private float _lifeTimer;

        private void Start()
        {
            _meshRenderer = GetComponent<MeshRenderer>();

            if (GraphicsSettings.renderPipelineAsset == null)
            {
                _initMatAlpha = _meshRenderer.material.color.a;
            }

            _fadeOutDuration = _initMatAlpha * 10f;
            _fadeOutTimer = 0;
            Debug.Assert(_meshRenderer != null);

            InvokeRepeating("Check", Random.Range(1f, 2f), 1f);
        }


        private void Fade()
        {
            float fadePercent = _fadeOutTimer / _fadeOutDuration;

            if (fadePercent >= 1f)
            {
                Destroy(gameObject);
            }
            else
            {
                if (GraphicsSettings.renderPipelineAsset == null)
                {
                    Material material = _meshRenderer.material;
                    Color color = material.color;
                    material.color = new Color(color.r, color.g, color.b, _initMatAlpha * Mathf.Clamp01(1f - fadePercent));
                }
            }

            _fadeOutTimer += 0.05f;
        }


        private void OnDestroy()
        {
            CancelInvoke();

            Destroy(GetComponent<MeshFilter>().sharedMesh);
            Destroy(GetComponent<MeshRenderer>().material);
        }


        private void Check()
        {
            if (targetTransform == null)
            {
                Destroy(gameObject);
                return;
            }

            bool distanceFlag = Vector3.Distance(transform.position, targetTransform.position) > distanceThreshold;
            bool timeFlag = timeThreshold > 0 && _lifeTimer >= timeThreshold;

            if (!skidmarkIsBeingUsed && (distanceFlag || timeFlag || destroyFlag))
            {
                InvokeRepeating("Fade", 0f, 0.05f);
            }

            _lifeTimer += 1f;
        }
    }
}