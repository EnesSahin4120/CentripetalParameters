#if UNITY_EDITOR
using NWH.Common.AssetInfo;
using UnityEditor;

namespace NWH.VehiclePhysics2
{
    public class InitializationMethodsNVP2 : CommonInitializationMethods
    {
        [InitializeOnLoadMethod]
        static void AddWC3DDefines()
        {
            AddDefines("NWH_WC3D");
        }

        [InitializeOnLoadMethod]
        static void ShowWC3DWelcomeWindow()
        {
            ShowWelcomeWindow("Wheel Controller 3D");
        }
    }
}
#endif