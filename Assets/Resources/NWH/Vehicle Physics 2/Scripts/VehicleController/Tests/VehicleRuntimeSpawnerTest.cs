using UnityEngine;

namespace NWH.VehiclePhysics2.Tests
{
    public partial class VehicleRuntimeSpawnerTest : MonoBehaviour
    {
        public GameObject vehicleToSpawn;
        public Vector3 position;
        private bool _spawned;


        private void Update()
        {
            if (!_spawned && Time.frameCount > 300)
            {
                _spawned = true;
                Instantiate(vehicleToSpawn, position, Quaternion.identity);
            }
        }
    }
}