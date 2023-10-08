using TMPro;
using UnityEngine;

public class UI_Manager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tangentialAcceleration_UI; 
    [SerializeField] private TextMeshProUGUI normalAcceleration_UI;
    [SerializeField] private TextMeshProUGUI curvatureRadius_UI; 

    public void SetUIValues(float tangentialAccel,  float normalAccel, float curvatureRadius)
    {
        tangentialAcceleration_UI.text = (Mathf.Floor(tangentialAccel * 100) / 100).ToString();
        normalAcceleration_UI.text = (Mathf.Floor(normalAccel * 100) / 100).ToString();
        curvatureRadius_UI.text = (Mathf.Floor(curvatureRadius * 100) / 100).ToString();
    }
}
