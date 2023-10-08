#if UNITY_EDITOR
using NWH.Common.AssetInfo;
using UnityEditor;

namespace NWH.VehiclePhysics2
{
    public class InitializationMethodsNVP2 : CommonInitializationMethods
    {
        [InitializeOnLoadMethod]
        static void AddNVP2Defines()
        {
            AddDefines("NWH_NVP2");
        }

        [InitializeOnLoadMethod]
        static void ShowNVP2WelcomeWindow()
        {
            ShowWelcomeWindow("NWH Vehicle Physics 2");
        }
    }
}
#endif