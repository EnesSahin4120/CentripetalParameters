using UnityEngine;

public class CentripetalParameters : MonoBehaviour
{
    [SerializeField] private LineDebug lineDebug;
    [SerializeField] private Transform vehicle;
    [SerializeField] private UI_Manager ui_Manager;

    //Centripetal Parameters
    private Vector3 prevPos;
    private float tangentialSpeed;
    private Vector3 tangentialSpeedVector;
    private Vector3 tangentialAccelerationVector;
    private Vector3 tangentialDir;
    public float tangentialAcceleration;
    private Vector3 normalAccelerationVector;
    public float normalAcceleration;
    private Vector3 normalAccelerationDir;
    private float relativeRadius;
    public float curvatureRadius;
    private Vector3 prevTangentialSpeedVector;
    private Vector3 curvatureCenter;

    private void FixedUpdate()
    {
        SetParameters();

        lineDebug.SetCurvature(100, curvatureRadius, curvatureCenter, vehicle.position);
        lineDebug.SetRadiusLine(curvatureRadius, curvatureCenter, vehicle.position);
        ui_Manager.SetUIValues(tangentialAcceleration, normalAcceleration, curvatureRadius);
    }

    private void SetParameters()
    {
        Vector3 deltaPosition = vehicle.position - prevPos;
        tangentialSpeedVector = deltaPosition / Time.fixedDeltaTime;
        tangentialSpeed = tangentialSpeedVector.sqrMagnitude;

        tangentialAccelerationVector = (tangentialSpeedVector - prevTangentialSpeedVector) / Time.fixedDeltaTime;
        tangentialAcceleration = tangentialAccelerationVector.sqrMagnitude;
        if(tangentialSpeed != 0.0f)
            tangentialDir = tangentialSpeedVector / tangentialSpeed;

        normalAccelerationVector = Vector3.Cross(Vector3.up, tangentialDir);
        normalAcceleration = Vector3.Dot(tangentialAccelerationVector, normalAccelerationVector);
        if(normalAcceleration != 0.0f)
        {
            normalAccelerationDir = normalAccelerationVector / normalAcceleration;
            relativeRadius = Vector3.Dot(tangentialSpeedVector, tangentialSpeedVector) / normalAcceleration;
        }
        curvatureCenter = vehicle.position + (normalAccelerationDir * Mathf.Abs(relativeRadius));
        curvatureRadius = Vector3.Distance(vehicle.position, curvatureCenter);

        prevPos = vehicle.position;
        prevTangentialSpeedVector = tangentialSpeedVector;
    }
}
