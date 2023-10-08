using UnityEngine;

public class LineDebug : MonoBehaviour
{
    [SerializeField] private LineRenderer curvatureRenderer;
    [SerializeField] private LineRenderer radiusLineRenderer;
    [SerializeField] private Transform centerDot;

    public void SetCurvature(int steps, float radius, Vector3 curvatureCenter, Vector3 vehiclePos)
    {
        DetectIfRender(radius);
        curvatureRenderer.positionCount = steps + 1;
        for (int currentStep = 0; currentStep <= steps; currentStep++)
        {
            float circumferenceProgress = (float)currentStep / steps;
            float currentRadian = circumferenceProgress * 2.0f * Mathf.PI;

            float xScaled = Mathf.Cos(currentRadian);
            float zScaled = Mathf.Sin(currentRadian);

            float x = xScaled * radius;
            float z = zScaled * radius;

            Vector3 currentPosition = curvatureCenter + new Vector3(x, vehiclePos.y, z);
            curvatureRenderer.SetPosition(currentStep, currentPosition);
        }
    }

    public void SetRadiusLine(float radius, Vector3 curvatureCenter, Vector3 vehiclePos)
    {
        DetectIfRender(radius);
        radiusLineRenderer.positionCount = 2;
        radiusLineRenderer.SetPosition(0, curvatureCenter);
        radiusLineRenderer.SetPosition(1, vehiclePos);
        centerDot.position = curvatureCenter;
    }

    private void DetectIfRender(float radius)
    {
        if(radius > 50.0f)
        {
            curvatureRenderer.enabled = false;
            radiusLineRenderer.enabled = false;
            centerDot.gameObject.SetActive(false);
        }
        else
        {
            curvatureRenderer.enabled = true;
            radiusLineRenderer.enabled = true;
            centerDot.gameObject.SetActive(true);
        }
    }
}
