using System.Collections.Generic;
using UnityEngine;

namespace NWH.WheelController3D
{
    public class StandardGroundDetection : GroundDetectionBase
    {
        private RaycastHit _nearestHit = new RaycastHit();
        private RaycastHit _castResult = new RaycastHit();
        private WheelController _wheelController;

        private Transform _transform;

#if WC3D_DEBUG
        private List<WheelCastResult> _wheelCastResults = new List<WheelCastResult>();
        private List<WheelCastInfo> _wheelCasts = new List<WheelCastInfo>();

        /// <summary>
        /// Used for debug gizmo drawing only.
        /// Holds ray/sphere cast data.
        /// </summary>
        [System.Serializable]
        private struct WheelCastInfo
        {
            public WheelCastInfo(Type castType, Vector3 origin, Vector3 direction,
                float distance, float radius, float width)
            {
                this.castType = castType;
                this.origin = origin;
                this.direction = direction;
                this.distance = distance;
                this.radius = radius;
                this.width = width;
            }

            public enum Type
            {
                Ray,
                Sphere
            }

            public Type castType;
            public Vector3 origin;
            public Vector3 direction;
            public float distance;
            public float radius;
            public float width;
        }

        /// <summary>
        /// Used for debug gizmo drawing only.
        /// Holds ray/sphere cast data.
        /// </summary>
        [System.Serializable]
        private struct WheelCastResult
        {
            public WheelCastResult(Vector3 point, Vector3 normal, WheelCastInfo castInfo)
            {
                this.point = point;
                this.normal = normal;
                this.castInfo = castInfo;
            }

            public Vector3 point;
            public Vector3 normal;
            public WheelCastInfo castInfo;
        }
#endif

        private void Awake()
        {
            _wheelController = GetComponent<WheelController>();
        }


        public override bool WheelCast(in Vector3 origin, in Vector3 direction, in float distance, in float radius, in float width, ref WheelHit wheelHit)
        {
            _transform = transform;

            bool initQueriesHitTriggers = Physics.queriesHitTriggers;
            bool initQueriesHitBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitTriggers = false;
            Physics.queriesHitBackfaces = false;


#if WC3D_DEBUG
            _wheelCastResults.Clear();
            _wheelCasts.Clear();
#endif

            bool isValid = false;
            if (WheelCastSingleSphere(origin, direction, distance, radius, width, ref _castResult))
            {
                if (IsInsideWheel(_castResult.point, origin, radius, width))
                {
                    isValid = true;
                }
                else
                {
                    if (WheelCastMultiSphere(origin, direction, distance, radius, width, ref _castResult))
                    {
                        // No need to check for inside wheel as the cast is to exact dimensions
                        isValid = true;
                    }
                }
            }


            if (isValid)
            {
                wheelHit.point = _castResult.point;
                wheelHit.normal = _castResult.normal;
                wheelHit.collider = _castResult.collider;
            }

            Physics.queriesHitTriggers = initQueriesHitTriggers;
            Physics.queriesHitBackfaces = initQueriesHitBackfaces;

            return isValid;
        }


        private bool WheelCastSingleSphere(in Vector3 origin, in Vector3 direction, in float distance, in float radius, in float width, ref RaycastHit hit)
        {
#if WC3D_DEBUG
            WheelCastInfo castInfo = new WheelCastInfo(WheelCastInfo.Type.Sphere,
              origin, direction, distance, radius, width);
            _wheelCasts.Add(castInfo);
#endif

            if (Physics.SphereCast(origin, radius, direction, out hit, distance))
            {
#if WC3D_DEBUG
                _wheelCastResults.Add(new WheelCastResult(hit.point, hit.normal, castInfo));
#endif

                return true;
            }

            return false;
        }


        private bool WheelCastMultiSphere(in Vector3 origin, in Vector3 direction, in float distance, in float radius, in float width, ref RaycastHit hit)
        {
            float nearestDistance = 1e10f;

            float subRadius = width * 0.5f;
            subRadius = Mathf.Clamp(subRadius, radius * 0.2f, radius);
            bool useRayInstead = width == 0 ? true : width * 10f < radius ? true : false;
            float zRange = 2f * radius;
            int zSteps = Mathf.RoundToInt(radius / subRadius) * 2;
            zSteps = zSteps % 2 == 0 ? zSteps + 1 : zSteps; // Ensure there is always a centered sphere.
            float stepAngle = 180f / (float)(zSteps - 1);

            Vector3 up = _transform.up;
            Vector3 right = _transform.right;
            Vector3 forward = _transform.forward;

            for (int z = 0; z < zSteps; z++)
            {
                Vector3 subOrigin = origin + Quaternion.AngleAxis(_wheelController.SteerAngle, up)
                    * Quaternion.AngleAxis(z * stepAngle, right) * forward * radius;

#if WC3D_DEBUG
                WheelCastInfo castInfo = new WheelCastInfo(useRayInstead ? WheelCastInfo.Type.Ray : WheelCastInfo.Type.Sphere,
                    subOrigin, direction, distance, subRadius, width);
                _wheelCasts.Add(castInfo);
#endif

                RaycastHit subHit;
                bool hasHit = useRayInstead ?
                    Physics.Raycast(subOrigin, direction, out subHit, distance) :
                    Physics.SphereCast(subOrigin, subRadius, direction, out subHit, distance);

                if (hasHit)
                {
                    Vector3 hitLocalPoint = _transform.InverseTransformPoint(subHit.point);
                    float hitAngle = Mathf.Asin(Mathf.Clamp(hitLocalPoint.z / radius, -1f, 1f));
                    float potentialWheelPosition = hitLocalPoint.y + radius * Mathf.Cos(hitAngle);
                    hit.distance = -potentialWheelPosition;

#if WC3D_DEBUG
                    _wheelCastResults.Add(new WheelCastResult(subHit.point, subHit.normal, castInfo));
#endif

                    if (hit.distance < nearestDistance)
                    {
                        nearestDistance = hit.distance;
                        _nearestHit = subHit;
                    }
                }
            }

            if (nearestDistance < 1e9f)
            {
                hit = _nearestHit;
                return true;
            }

            return false;
        }


        private bool IsInsideWheel(in Vector3 point, in Vector3 wheelPos, in float radius, in float width)
        {
            Vector3 offset = point - wheelPos;
            Vector3 localOffset = _transform.InverseTransformVector(offset);
            float halfWidth = width * 0.5f;
            if (localOffset.x >= -halfWidth && localOffset.x <= halfWidth
                && localOffset.z <= radius && localOffset.z >= -radius)
            {
                return true;
            }

            return false;
        }


        private void OnDrawGizmos()
        {
#if WC3D_DEBUG
            foreach (WheelCastInfo wheelCast in _wheelCasts)
            {
                Gizmos.color = Color.cyan;
                if (wheelCast.castType == WheelCastInfo.Type.Sphere)
                {
                    Gizmos.DrawWireSphere(wheelCast.origin, wheelCast.radius);
                }
                else
                {
                    Gizmos.DrawCube(wheelCast.origin, new Vector3(0.01f, 0.05f, 0.01f));
                }

                Gizmos.DrawRay(wheelCast.origin, wheelCast.direction * wheelCast.distance);
            }

            foreach (WheelCastResult result in _wheelCastResults)
            {
                bool isInsideWheel = IsInsideWheel(result.point, result.castInfo.origin,
                    result.castInfo.radius, result.castInfo.width);
                Gizmos.color = isInsideWheel ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(result.point, 0.02f);
                Gizmos.DrawRay(result.point, result.normal * 0.1f);

                Gizmos.color = Color.blue;
                Gizmos.DrawRay(result.point, transform.forward * 0.2f);
            }
#endif
        }
    }
}

