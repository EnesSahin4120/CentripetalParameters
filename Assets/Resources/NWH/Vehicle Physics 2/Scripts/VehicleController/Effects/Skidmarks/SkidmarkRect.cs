using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Effects
{
    /// <summary>
    ///     One section (rectangle) of the skidmark.
    /// </summary>
    [Serializable]
    public struct SkidmarkRect
    {
        public Vector3 normal;
        public Vector3 position;
        public Vector3 positionLeft;
        public Vector3 positionRight;
        public Vector4 tangent;
    }
}