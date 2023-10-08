using System;
using System.Collections.Generic;
using UnityEngine;

namespace NWH.VehiclePhysics2
{
    public abstract class ManagerVehicleComponent : VehicleComponent
    {
        /// <summary>
        ///     All effects are placed in this list after initialization.
        /// </summary>
        [Tooltip("    All effects are placed in this list after initialization.")]
        [NonSerialized]
        protected List<VehicleComponent> _components = null;

        public abstract List<VehicleComponent> Components { get; }


        public override void Start(VehicleController vc)
        {
            base.Start(vc);

            for (int i = 0; i < Components.Count; i++)
            {
                VehicleComponent component = Components[i];
                component.Start(vc);
            }
        }


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            for (int i = 0; i < Components.Count; i++)
            {
                VehicleComponent component = Components[i];
                component.Update();
            }
        }

        public override void FixedUpdate()
        {
            if (!Active)
            {
                return;
            }

            foreach (VehicleComponent component in Components)
            {
                component.FixedUpdate();
            }
        }


        public override void OnDrawGizmosSelected(VehicleController vc)
        {
            base.OnDrawGizmosSelected(vc);

            for (int i = 0; i < Components.Count; i++)
            {
                VehicleComponent component = Components[i];
                component.OnDrawGizmosSelected(vc);
            }
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            for (int i = 0; i < Components.Count; i++)
            {
                VehicleComponent component = Components[i];
                component.SetDefaults(vc);
            }
        }


        public override void CheckState(int lodIndex)
        {
            base.CheckState(lodIndex);

            for (int i = 0; i < Components.Count; i++)
            {
                VehicleComponent component = Components[i];
                component.CheckState(lodIndex);
            }
        }
    }
}

