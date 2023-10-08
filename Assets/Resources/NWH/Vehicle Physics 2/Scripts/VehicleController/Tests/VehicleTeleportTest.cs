using UnityEngine;

namespace NWH.VehiclePhysics2.Tests
{
    public partial class VehicleTeleportTest : MonoBehaviour
    {
        public VehicleController targetVehicle;

        private Vector3 _initPos;
        private float _timer;


        private void Awake()
        {
            _initPos = targetVehicle.transform.position;
        }


        private void Update()
        {
            _timer += Time.deltaTime;

            if (_timer > 5f)
            {
                targetVehicle.transform.position = new Vector3(0, targetVehicle.transform.position.y, 0);
                _timer = 0;
            }
        }
    }
}