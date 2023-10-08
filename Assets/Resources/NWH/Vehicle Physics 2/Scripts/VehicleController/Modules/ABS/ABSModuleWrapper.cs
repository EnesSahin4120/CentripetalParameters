using System;

namespace NWH.VehiclePhysics2.Modules.ABS
{
    /// <summary>
    ///     MonoBehaviour wrapper for ABS module.
    /// </summary>
    [Serializable]
    public partial class ABSModuleWrapper : ModuleWrapper
    {
        public ABSModule module = new ABSModule();


        public override VehicleModule GetModule()
        {
            return module;
        }


        public override void SetModule(VehicleModule module)
        {
            this.module = module as ABSModule;
        }
    }
}